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

                # Receive image size
                img_size_bytes = conn.recv(4)
                img_size_little = int.from_bytes(img_size_bytes,'little')

                # Receive image in packages
                i = 0
                while i < img_size_little:
                    packet = conn.recv(4096)  # Adjust the buffer size as needed
                    if not packet:
                        break
                    img_data += packet
                    i += len(packet)

                print(len(img_data))

                # Decode byte array to cv2 image
                decoded = np.frombuffer(img_data, dtype=np.uint8)
                image = cv2.imdecode(decoded, cv2.IMREAD_COLOR)
                cv2.imwrite("received_image.png", image)

                # Send Reveive message
                message = bytes("Received", 'utf-8')
                conn.sendall(message)

                # Receive coordinates
                coord_data = conn.recv(8)
                if not coord_data:
                    break

                # Decode coorindates into floats
                vector = struct.unpack('ff', coord_data)
                # conn.sendall(img_data)
        
                print(vector)

                # Send mask back
                mask = np.random.rand(1000,700)
                mask_boolean = mask>=0.5
                
                rows = mask_boolean.shape[0].to_bytes(4,'little')
                conn.sendall(rows)
                #print(rows)

                cols = mask_boolean.shape[1].to_bytes(4,'little')
                conn.sendall(cols)
                #print(cols)

                for row in mask_boolean:
                     bytes_row = bytes(map(lambda x: 1 if x else 0, row))
                     #print(bytes_row)
                     conn.sendall(bytes_row)


        conn.close()
            #s.close()
        
    s.close
