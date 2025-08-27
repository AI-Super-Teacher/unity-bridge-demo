using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using UnityEngine;

public class Bridge : MonoBehaviour
{
    [SerializeField] private Animator animator; // set in Inspector (optional)

    private ClientWebSocket _ws;
    private CancellationTokenSource _cts;
    private readonly ConcurrentQueue<Action> _mainQueue = new();
    [SerializeField] private CoinSpawner coinSpawner; 

    void Awake() {
         Debug.Log("[Boot] Bridge component awake; about to connect...");
        if (coinSpawner == null) coinSpawner = FindObjectOfType<CoinSpawner>();

        if (animator == null) {
            // Try the common places
            animator = FindObjectOfType<Animator>(); // or a more specific lookup
        }
    }

    void Start()
    {
        var url = ParseBridgeUrl();  // e.g., ws://127.0.0.1:7012/bridge?token=...
        if (string.IsNullOrEmpty(url)) { Debug.LogError("[Bridge] Missing --bridge-url"); return; }
        Debug.Log("[Bridge] Will connect to: " + url);
        _cts = new CancellationTokenSource();
        _ = ConnectLoop(url, _cts.Token);
    }


    string ParseBridgeUrl() {
        foreach (var a in Environment.GetCommandLineArgs()) {
            Debug.Log("[Bridge] Command line argument: " + a);
            if (a.StartsWith("--bridge-url=")) return a.Substring("--bridge-url=".Length);
        }
        return null;
    }

    void Update()
    {
        while (_mainQueue.TryDequeue(out var a)) a?.Invoke();
    }

    void OnDestroy() { try { _cts?.Cancel(); } catch {} _ws?.Dispose(); }

    (string url, string token) ParseArgs()
    {
        string u = null, t = null;
        foreach (var a in Environment.GetCommandLineArgs())
        {
            if (a.StartsWith("--bridge-url=")) u = a.Substring("--bridge-url=".Length);
            else if (a.StartsWith("--bridge-token=")) t = a.Substring("--bridge-token=".Length);
        }
        if (!string.IsNullOrEmpty(u) && !string.IsNullOrEmpty(t))
            u = $"{u}?token={t}";
        return (u, t);
    }


    async Task ConnectLoop(string url, CancellationToken ct) {
        while (!ct.IsCancellationRequested) {
            try {
                _ws = new ClientWebSocket();
                // Optional keep-alive
                _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

                await _ws.ConnectAsync(new Uri(url), ct);
                Debug.Log("[Bridge] Connected");

                // Send a hello
                await SendJson(new { type = "hello", from = "unity" }, ct);

                // Receive loop
                var buf = new ArraySegment<byte>(new byte[16 * 1024]);
                while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested) {
                    var sb = new StringBuilder();
                    WebSocketReceiveResult r;
                    do {
                        r = await _ws.ReceiveAsync(buf, ct);
                        if (r.MessageType == WebSocketMessageType.Close) {
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct);
                            break;
                        }
                        sb.Append(Encoding.UTF8.GetString(buf.Array, 0, r.Count));
                    } while (!r.EndOfMessage);

                    var json = sb.ToString();
                    if (!string.IsNullOrEmpty(json)) HandleMessage(json);
                }
            }
            catch (Exception e) {
                Debug.LogWarning("[Bridge] Connect error: " + e.Message);
            }

            // Backoff before retry
            await Task.Delay(1500, ct);
        }
    }

    async Task ConnectAndPumpAsync(string url, string token, CancellationToken ct)
    {
        _ws = new ClientWebSocket();
        try {
            var uri = new Uri(url);
            await _ws.ConnectAsync(uri, ct);
            Debug.Log("[Bridge] Connected " + url);

            var recv = ReceiveLoop(ct);
            // Optionally send a hello:
            await SendJson(new { type = "hello", from = "unity" }, ct);
            await recv;
        }
        catch (Exception e) {
            Debug.LogError("[Bridge] Connect error: " + e.Message);
        }
    }

    async Task ReceiveLoop(CancellationToken ct)
    {
        var buf = new ArraySegment<byte>(new byte[16 * 1024]);
        var sb = new StringBuilder();

        while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            sb.Clear();
            WebSocketReceiveResult r;
            do {
                r = await _ws.ReceiveAsync(buf, ct);
                if (r.MessageType == WebSocketMessageType.Close) {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct);
                    return;
                }
                sb.Append(Encoding.UTF8.GetString(buf.Array, 0, r.Count));
            } while (!r.EndOfMessage);

            var json = sb.ToString();
            HandleMessage(json);
        }
    }

    void HandleMessage(string json)
    {
        try {
            var msg = JsonUtility.FromJson<Msg>(json);
            Debug.Log("[Bridge] Received message: " + msg.type + " " + msg.name + " " + msg.value);
            switch (msg.type)
            {
                case "trigger":
                    if (animator && !string.IsNullOrEmpty(msg.name))
                        _mainQueue.Enqueue(() => animator.SetTrigger(msg.name));
                    break;
                case "float":
                    if (animator && !string.IsNullOrEmpty(msg.name))
                        _mainQueue.Enqueue(() => animator.SetFloat(msg.name, msg.value));
                    break;
                case "bool":
                    if (animator && !string.IsNullOrEmpty(msg.name))
                        _mainQueue.Enqueue(() => animator.SetBool(msg.name, msg.value > 0.5f));
                    break;
                case "spawn":
                    _mainQueue.Enqueue(() => coinSpawner?.Spawn(10));
                    break;

                // Add more message types as needed
            }
        }
        catch (Exception e) {
            Debug.LogWarning($"[Bridge] Bad JSON: {json} err={e.Message}");
        }
    }

    public async Task SendJson(object obj, CancellationToken ct) {
        if (_ws == null || _ws.State != WebSocketState.Open) return;
        var data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj));
        await _ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, ct);
    }

    [Serializable]
    private class Msg { public string type; public string name; public float value;  }
}
