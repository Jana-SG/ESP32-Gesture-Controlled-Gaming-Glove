#include <Wire.h>

const int MPU_ADDR = 0x68;  // MPU6050 I2C address
int16_t gyroX, gyroY, gyroZ;

const int flexPin = 34;
const int shootThreshold = 100;  
bool lastFlexState = false;
unsigned long lastShootTime = 0;
unsigned long shootCooldown = 250;

void setup() {
  Serial.begin(115200);
  Wire.begin(21, 22); // SDA = 21, SCL = 22

  // Wake up MPU6050
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x6B); 
  Wire.write(0);  
  Wire.endTransmission(true);

  delay(1000);
  Serial.println("Ready! Reading MPU6050 and Flex Sensor...");
}

void loop() {
  // --- Read Gyroscope data ---
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(0x43); // Starting register for gyro data
  Wire.endTransmission(false);
  Wire.requestFrom(MPU_ADDR, 6, true);

  if (Wire.available() == 6) {
    gyroX = Wire.read() << 8 | Wire.read();
    gyroY = Wire.read() << 8 | Wire.read();
    gyroZ = Wire.read() << 8 | Wire.read();

    Serial.print(gyroX);
    Serial.print(",");
    Serial.print(gyroY);
    Serial.print(",");
    Serial.println(gyroZ);
  } else {
    Serial.println("0,0,0");  // If communication fails, send zeros
  }

  // --- Flex Sensor Logic ---
  int flexValue = analogRead(flexPin);
  bool isFlexed = (flexValue > shootThreshold);
  unsigned long now = millis();

  if (isFlexed && !lastFlexState && now - lastShootTime > shootCooldown) {
    Serial.println("Shoot");
    lastShootTime = now;
  }

  lastFlexState = isFlexed;
  delay(200); // Avoid flooding the serial port
}
