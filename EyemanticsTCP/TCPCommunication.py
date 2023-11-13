import socket
import numpy as np
import cv2
import struct

IPaddr = "127.0.0.1"
port = 4350

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    #s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    s.bind((IPaddr, port))
    while(True):
        s.listen()
        conn, addr = s.accept()
        with conn:
            print(f"Connected by {addr}")
            while True:
                img_data = b""
                img_size_bytes = conn.recv(4)
                img_size_little = int.from_bytes(img_size_bytes,'little')

                # length_data = conn.recv(4)
                # if not length_data:
                #     break
                # length = struct.unpack('!I', length_data)[0]


                # message = bytes("Hello", 'utf-8')
                # conn.sendall(message)

                i = 0
                while i < img_size_little:
                    packet = conn.recv(4096)  # Adjust the buffer size as needed
                    if not packet:
                        break
                    # if packet == delimter:
                    #     break
                    img_data += packet
                    i += len(packet)

                print(len(img_data))

                decoded = np.frombuffer(img_data, dtype=np.uint8)
                image = cv2.imdecode(decoded, cv2.IMREAD_COLOR)
                cv2.imwrite("received_image.png", image)

                message = bytes("Hello", 'utf-8')
                conn.sendall(message)

                # print(message)
                coord_data = conn.recv(8)
                if not coord_data:
                    break
                vector = struct.unpack('ff', coord_data)
                # conn.sendall(img_data)
        
                print(vector)

        conn.close()
            #s.close()
        
    s.close
