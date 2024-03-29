﻿using Bitfinex.Client.Websocket.Responses.Books;
using Microsoft.AspNetCore.SignalR.Client;

namespace OrderBookClientHW.Services;

public class OrderBookHubClientService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private Dictionary<string, Book> _ordersBookAsk = new Dictionary<string, Book>();
    private Dictionary<string, Book> _ordersBookBid = new Dictionary<string, Book>();
    private readonly ConnectionStatusService _connectionStatusService;
    private readonly string _hubUrl;
    
    public OrderBookHubClientService(ConnectionStatusService connectionStatusService, string hubUrl)
    {
        _hubUrl = hubUrl;
        _connectionStatusService = connectionStatusService;
    }
    
    public async Task InitializeAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .Build();

        _hubConnection.On<Dictionary<string, Book>>("ReceiveBookOrderAsk", books =>
        {
            _ordersBookAsk.Clear();
            foreach (var book in books)
            {
                _ordersBookAsk.Add(book.Key, book.Value);
            }
            NotifyStateAskChanged();
        });

        _hubConnection.On<Dictionary<string,Book>>("ReceiveBookOrderBid", books =>
        {
            _ordersBookBid.Clear();
            foreach (var book in books)
            {
                _ordersBookBid.Add(book.Key, book.Value);
            }
            NotifyStateBidChanged();
        });

        await _hubConnection.StartAsync();
        _connectionStatusService.IsConnected = _hubConnection.State == HubConnectionState.Connected;
    }

    public Dictionary<string, Book> GetAsk()
    {
        return _ordersBookAsk;
    }

    public Dictionary<string, Book> GetBid()
    {
        return _ordersBookBid;
    }

    public event Action? OnChangeAsk;
    public event Action? OnChangeBid;

    private void NotifyStateAskChanged() => OnChangeAsk?.Invoke();
    private void NotifyStateBidChanged() => OnChangeBid?.Invoke();

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            _connectionStatusService.IsConnected = false;
            await _hubConnection.DisposeAsync();
        }
    }
}