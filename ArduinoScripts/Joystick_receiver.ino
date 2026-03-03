// 设备：按钮 + 摇杆 (ESP32-S3 专用版)
// 已清洗所有特殊字符，确保编译通过

// --- 引脚定义 (针对 ESP32-S3 优化) ---
// S3如果开启PSRAM，GPIO35/36/37可能会有问题
// 强烈建议使用 ADC1 通道的 GPIO 1~10
const int JOY_X_PIN = 4;   // 请将 VRx 接到 GPIO 4
const int JOY_Y_PIN = 5;   // 请将 VRy 接到 GPIO 5
const int BUTTON_PIN = 14; // 请将 SW  接到 GPIO 14

// 阈值设置
const int THRESHOLD_LOW  = 1500;
const int THRESHOLD_HIGH = 2500;

// 心跳包发送间隔
const int DEVICE_ID_INTERVAL = 40; 
int loopCount = 0;

void setup() {
  Serial.begin(115200);
  delay(1000); 

  // 设置ADC分辨率 (ESP32 S3 支持 12位，即 0-4095)
  analogReadResolution(12);

  pinMode(JOY_X_PIN, INPUT);
  pinMode(JOY_Y_PIN, INPUT);
  pinMode(BUTTON_PIN, INPUT_PULLUP); 

  Serial.println("[DEVICE:BUTTON&JOYSTICK]");
}

void loop() {
  int x = analogRead(JOY_X_PIN);
  int y = analogRead(JOY_Y_PIN);

  // 逻辑判断
  bool up     = (y < THRESHOLD_LOW);
  bool down   = (y > THRESHOLD_HIGH);
  bool left   = (x < THRESHOLD_LOW);
  bool right  = (x > THRESHOLD_HIGH);
  // 按钮是上拉模式，低电平代表按下
  bool btnPressed = (digitalRead(BUTTON_PIN) == LOW);

  // 发送数据帧
  // Serial.printf 是 ESP32 特有功能
  Serial.printf("JS:%d,%d,%d,%d:%d\n",
    up ? 1 : 0,
    down ? 1 : 0,
    left ? 1 : 0,
    right ? 1 : 0,
    btnPressed ? 1 : 0
  );

  // 定期发送心跳包 (用于 Unity 识别)
  loopCount++;
  if (loopCount >= DEVICE_ID_INTERVAL) {
    Serial.println("[DEVICE:BUTTON&JOYSTICK]");
    loopCount = 0;
  }

  delay(30); 
}