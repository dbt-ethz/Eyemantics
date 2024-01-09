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
    # image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB) #test this
    predictor.set_image(image)
    input_label = np.array([1])  # foreground
    mask, _, _ = predictor.predict(
        point_coords=input_point,
        point_labels=input_label,
        multimask_output=False,
        )
    return mask

print("Initializing SAM...")
sam_checkpoint = "weights/sam_vit_b_01ec64.pth"
model_type = "vit_b"
device = "cpu" # "cpu" if no gpu, "cuda" if gpu available

sam = sam_model_registry[model_type](checkpoint=sam_checkpoint)
sam.to(device=device)

predictor = SamPredictor(sam)

IPaddr = input('Input the MagicLeap device IP:\n') 

# IPaddr = "127.0.0.1" #change this
port = 4350


with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:

    while(True):
        s.connect((IPaddr,port))
        print("Magic Leap 2 connected!")
        while True:
            try:
                img_data = b""

                # Receive image size
                img_size_bytes = s.recv(4)
                img_size_little = int.from_bytes(img_size_bytes,'little')

                # Receive image in packages
                i = 0
                while i < img_size_little:
                    packet = s.recv(4096)
                    if not packet:
                        break
                    img_data += packet
                    i += len(packet)

                # Get coordinates
                vec_data = img_data[-8:]
                vector = struct.unpack('ff', vec_data)

                # Get image data
                img_data = img_data[:-8]

                # Decode byte array to cv2 image
                decoded = np.frombuffer(img_data, dtype=np.uint8)
                image = cv2.imdecode(decoded, cv2.IMREAD_COLOR)

                print("Received Image")

                # print("generating mask...")

                vector = np.array(vector).astype(int)
                vector = np.array([vector])
                print(f"Received gaze point: {vector}")
                # print(vector)

                if (vector == np.array([-1 -1])).all():
                    print("Invalid Gaze Point")
                    continue

                # Accessing the coordinates from the nested array format
                x_coord = int(vector[0][0])
                y_coord = int(vector[0][1])
                image_with_gaze = image.copy()

                # Draw a red dot at the received coordinates
                cv2.circle(image_with_gaze, (x_coord, y_coord), radius=20, color=(0, 0, 255), thickness=-1)

                filename = 'ML2Image.jpg'
                
                # Using cv2.imwrite() method 
                # Saving the image 
                cv2.imwrite(filename, image_with_gaze)

                mask_boolean = image_to_mask(predictor, vector, image)

                mask_boolean = np.squeeze(mask_boolean)

                # Convert to binary array
                binary_image = np.uint8(mask_boolean)

                binary_image *= 255

                # Save the binary image using cv2.imwrite (optional)
                cv2.imwrite("mask_image.png", binary_image)

                # Send mask back
                for row in mask_boolean:
                        bytes_row = bytes(map(lambda x: 1 if x else 0, row))
                        # print(bytes_row)
                        s.sendall(bytes_row)

                print("Done sending segmentation mask")
            
            except:
                print("Device disconnected. Is the app running on the Magic Leap 2.")
                exit()

        s.close()