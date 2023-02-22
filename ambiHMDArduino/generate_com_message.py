import random


led_count_per_eye = 8

brightness = random.randint(0, 100)
leds = []

for i in range(led_count_per_eye):
    randomColor = (random.randint(0, 255), random.randint(0, 255), random.randint(0, 255))
    leds.append(randomColor)

message = f"{str(brightness).zfill(3)}"

for led in leds:
    r = str(led[0]).zfill(3)
    g = str(led[1]).zfill(3)
    b = str(led[2]).zfill(3)
    message += f"{r}{g}{b}"

print(message)