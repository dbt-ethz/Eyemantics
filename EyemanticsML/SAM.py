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

print("initializing SAM...")
sam_checkpoint = "weights/sam_vit_b_01ec64.pth"
model_type = "vit_b"
device = "cpu" # "cpu" if no gpu, "cuda" if gpu available

sam = sam_model_registry[model_type](checkpoint=sam_checkpoint)
sam.to(device=device)

predictor = SamPredictor(sam)

#IPaddr = input('Input the MagicLeap device IP\n') 

IPaddr = "192.168.1.61" #change this
port = 4350

firstIter = False

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    while(True):
        s.connect((IPaddr,port))
        print("connected!")
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



            if not firstIter:
                vec_data = img_data[-8:]
                vector_test = struct.unpack('ff', vec_data)
                print(vector_test)

                img_data = img_data[:-8]

            print(f"received image of size: {len(img_data)}")

            # Decode byte array to cv2 image
            decoded = np.frombuffer(img_data, dtype=np.uint8)
            image = cv2.imdecode(decoded, cv2.IMREAD_COLOR)

            # cv2.imshow('Magic Leap Camera Image', image)



            if firstIter:

                # # Send Reveive message
                # message = bytes("Received", 'utf-8')
                # s.sendall(message)

                # Receive coordinates
                coord_data = s.recv(8)
                if not coord_data:
                    break


                # Decode coorindates into floats
                vector = struct.unpack('ff', coord_data)
                # conn.sendall(img_data)

            else:
                vector = vector_test
    
            print(f"received gaze point: {vector}")

            print("generating mask...")

            vector = np.array(vector).astype(int)
            vector = np.array([vector])
            print(vector)

            if (vector == np.array([[-1 -1]])).all():
                print("Invalid Gaze Point")
                break

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

            binary_image = np.uint8(mask_boolean)

            for row in binary_image:
                # Calculate the sum of the current row
                row_sum = np.sum(row)
                
                # Check if the sum is greater than 0
                #if row_sum > 0:
                    #print(f"Sum of row: {row_sum}")

            binary_image *= 255

            # Display the binary image using cv2.imshow (optional)
            # cv2.imshow("Binary Image", binary_image)
            # cv2.waitKey(0)
            # cv2.destroyAllWindows()

            # Save the binary image using cv2.imwrite (optional)
            cv2.imwrite("mask_image.png", binary_image)

            test = np.sum(binary_image)
            print(test)

            # Send mask back
            print("sending mask back...")
            rows = mask_boolean.shape[0].to_bytes(4,'little')
            # s.sendall(rows)
            print(rows)

            cols = mask_boolean.shape[1].to_bytes(4,'little')
            # s.sendall(cols)
            print(cols)

            for row in mask_boolean:
                    bytes_row = bytes(map(lambda x: 1 if x else 0, row))
                    # print(bytes_row)
                    s.sendall(bytes_row)




            print("Done sending")

        s.close()


            #s.close()
        
