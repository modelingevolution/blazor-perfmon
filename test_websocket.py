#!/usr/bin/env python3
import websocket
import msgpack
import time

def on_message(ws, message):
    try:
        data = msgpack.unpackb(message, raw=False)
        timestamp = data.get('TimestampMs', 0)
        cpu_loads = data.get('CpuLoads', [])

        print(f"[{timestamp}] Received CPU data for {len(cpu_loads)} cores:")
        for i, load in enumerate(cpu_loads):
            print(f"  CPU{i}: {load:.1f}%")
        print()
    except Exception as e:
        print(f"Error deserializing: {e}")
        print(f"Raw message length: {len(message)} bytes")

def on_error(ws, error):
    print(f"Error: {error}")

def on_close(ws, close_status_code, close_msg):
    print("Connection closed")

def on_open(ws):
    print("WebSocket connected! Listening for CPU metrics...")

if __name__ == "__main__":
    print("Connecting to WebSocket at ws://localhost:5062/ws")
    ws = websocket.WebSocketApp("ws://localhost:5062/ws",
                                on_open=on_open,
                                on_message=on_message,
                                on_error=on_error,
                                on_close=on_close)

    try:
        ws.run_forever()
    except KeyboardInterrupt:
        print("\nStopping...")
