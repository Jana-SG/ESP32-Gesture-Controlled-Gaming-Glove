# ESP32 Gesture-Controlled Gaming Glove

A wearable gesture-based gaming controller built using:

- ESP32
- MPU6050 (accelerometer + gyroscope)
- Flex sensor
- Python bridge
- Unity game integration

This project allows hand gestures to control player movement and shooting in a Unity game.

---

##  System Architecture

ESP32 (MPU6050 + Flex)  
→ UDP transmission  
→ Python bridge (`serial_reader.ipynb`)  
→ Unity reads gesture data  
→ Player movement + shooting

---

## 📁 Repository Structure
```
/
├── unity/
│   ├── Scripts/
│   │   ├── RigidbodyFirstPersonController.cs
│   │   ├── SerialController.cs
│   │   └── Wepon.cs
│   
├── python/
│   └── serial_reader.ipynb
│
├── arduino/
│   └── esp32_glove.ino
│
└── docs/
    ├── Gesture controlled gaming glove.pdf
    └── Gesture controlled gaming glove.pptx
```

