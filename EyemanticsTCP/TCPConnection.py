import socket
import numpy as np
import cv2
import struct
from PIL import Image 

IPaddr = "127.0.0.1"
port = 4350

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.bind((IPaddr, port))
    while(True):
        s.listen()
        conn, addr = s.accept()
        with conn:
            print(f"Connected by {addr}")
            while True:
                # Receive Image data
                img_data = b""
                while True:
                    packet = conn.recv(4096)  # Adjust the buffer size as needed
                    if not packet:
                        break
                    img_data += packet
                if not img_data:
                    break

                # Convert the byte data back to an image
                decoded = np.frombuffer(img_data, dtype=np.uint8)
                image = cv2.imdecode(decoded, cv2.IMREAD_COLOR)
                cv2.imwrite("received_image.png", image)

                # Receive coordinate data
                coord_data = conn.recv(8)
                if not coord_data:
                  break
                vector = struct.unpack('ff', coord_data)

                
                # DO SEGMENTATION
                mask =img_data.Copy()


                # Send mask
                pckSize = 4096
                i = 0
                while(i < len(mask)):
                    remainingBytes = min(pckSize,len(mask) - i)
                    pck = mask[i:i+remainingBytes]
                    conn.sendall(pck)

            s.close()
    s.close