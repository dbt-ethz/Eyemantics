import numpy as np
import torch
import cv2
import time
import socket
import struct
from build_sam import sam_model_registry
from predictor import SamPredictor
from automatic_mask_generator import *


def image_to_mask(pedictor: any, input_point: np.ndarray, image: np.ndarray) -> np.ndarray:
    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    predictor.set_image(image)
    input_label = np.array([1])  # foreground
    mask, _, _ = predictor.predict(
        point_coords=input_point,
        point_labels=input_label,
        multimask_output=False,
        )
    return mask


sam_checkpoint = "weights/sam_vit_b_01ec64.pth"
model_type = "vit_b"
device = "cpu" # "cpu" if no gpu, "cuda" if gpu available

sam = sam_model_registry[model_type](checkpoint=sam_checkpoint)
sam.to(device=device)

predictor = SamPredictor(sam)


IPaddr = "127.0.0.1"
port = 4350

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    while(True):
        s.connect((IPaddr,port))

        while True:
            img_data = b""

            # Receive image size
            img_size_bytes = s.recv(4)
            img_size_little = int.from_bytes(img_size_bytes,'little')

            # Receive image in packages
            i = 0
            while i < img_size_little:
                packet = s.recv(4096)  # Adjust the buffer size as needed
                if not packet:
                    break
                img_data += packet
                i += len(packet)

            print(f"received image of size: {len(img_data)}")

            # Decode byte array to cv2 image
            decoded = np.frombuffer(img_data, dtype=np.uint8)
            image = cv2.imdecode(decoded, cv2.IMREAD_COLOR)

            # Send Reveive message
            message = bytes("Received", 'utf-8')
            s.sendall(message)

            # Receive coordinates
            coord_data = s.recv(8)
            if not coord_data:
                break

            # Decode coorindates into floats
            vector = struct.unpack('ff', coord_data)
            # conn.sendall(img_data)
    
            print(f"received gaze point: {vector}")

            print("generating mask...")
            mask_boolean = path_to_mask(predictor, vector, image)

            # Send mask back
            print("sending mask back...")
            rows = mask_boolean.shape[0].to_bytes(4,'little')
            s.sendall(rows)
            #print(rows)

            cols = mask_boolean.shape[1].to_bytes(4,'little')
            s.sendall(cols)
            #print(cols)

            for row in mask_boolean:
                    bytes_row = bytes(map(lambda x: 1 if x else 0, row))
                    #print(bytes_row)
                    s.sendall(bytes_row)


        s.close()
            #s.close()
        