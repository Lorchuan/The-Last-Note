import socket
import threading
import sys
try:
    import msvcrt  # Windows
except ImportError:
    import select  # Unix/Linux

def start_python_client():
    server_ip = "127.0.0.1"
    server_port = 5000
    running = True

    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client_socket.bind((server_ip, server_port))
    client_socket.listen(1)
    print("Python client listening on port 5000...")
    print("Press ESC to stop the server...")

    # 键盘监听线程
    def keyboard_listener():
        nonlocal running
        while running:
            # Windows系统
            if 'msvcrt' in sys.modules:
                if msvcrt.kbhit():
                    key = msvcrt.getch()
                    if key == b'\x1b':  # ESC键
                        running = False
                        print("\nESC pressed, shutting down...")
                        client_socket.close()
            # Unix/Linux系统
            else:
                if select.select([sys.stdin], [], [], 0)[0]:
                    key = sys.stdin.read(1)
                    if key == '\x1b':  # ESC键
                        running = False
                        print("\nESC pressed, shutting down...")
                        client_socket.close()

    # 启动键盘监听线程
    keyboard_thread = threading.Thread(target=keyboard_listener, daemon=True)
    keyboard_thread.start()

    try:
        while running:
            try:
                conn, addr = client_socket.accept()
            except OSError:
                break  # 当socket关闭时退出

            print(f"Connected to Unity at {addr}")
            data = conn.recv(1024)
            if data:
                message = data.decode("utf-8")
                print(f"Received message from Unity: {message}")

                if message == "StartMovingForward":
                    print("Object is moving forward!")

            conn.close()
    finally:
        if running:  # 确保资源被清理
            client_socket.close()
        print("Server shutdown complete.")

if __name__ == "__main__":
    start_python_client()
