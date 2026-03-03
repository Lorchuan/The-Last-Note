// MPU6050 轨迹预处理 + 校准 + 滤波
// 适用于 ESP32-S3 (SDA=12, SCL=11)
// 输出格式: acc_x,acc_y,acc_z,gyro_x,gyro_y,gyro_z (单位: m/s², rad/s)

#include <Adafruit_MPU6050.h>
#include <Adafruit_Sensor.h>
#include <Wire.h>

#define SDA_PIN 12
#define SCL_PIN 11

// 滑动平均窗口大小（建议 5~10）
#define FILTER_SIZE 8

enum Axis { X, Y, Z };

Adafruit_MPU6050 mpu;
float acc_bias[3] = {0, 0, 0};
bool calibrated = false;

// 滑动平均缓冲区
float acc_buffer[FILTER_SIZE][3] = {0};
float gyro_buffer[FILTER_SIZE][3] = {0};
int buffer_index = 0;

void setup() {
  Serial.begin(115200);
  while (!Serial) delay(10);

  Serial.println("MPU6050 Trajectory Preprocessor");
  Serial.println("Send 'c' to calibrate (device must be still!)");

  Wire.begin(SDA_PIN, SCL_PIN);

  if (!mpu.begin(MPU6050_I2CADDR_DEFAULT, &Wire)) {
    Serial.println("Failed to find MPU6050!");
    while (1) delay(1000);
  }

  // 配置传感器
  mpu.setAccelerometerRange(MPU6050_RANGE_8_G);
  mpu.setGyroRange(MPU6050_RANGE_500_DEG);
  mpu.setFilterBandwidth(MPU6050_BAND_21_HZ); // 低通滤波，减少噪声

  delay(100);
}

// 执行一次偏置校准（需静止）
void performCalibration() {
  Serial.println("Calibrating... Keep device STILL!");
  float sum[3] = {0, 0, 0};
  const int samples = 50;

  for (int i = 0; i < samples; i++) {
    sensors_event_t a, g, temp;
    mpu.getEvent(&a, &g, &temp);
    sum[X] += a.acceleration.x;
    sum[Y] += a.acceleration.y;
    sum[Z] += a.acceleration.z;
    delay(10);
  }

  acc_bias[X] = sum[X] / samples;
  acc_bias[Y] = sum[Y] / samples;
  acc_bias[Z] = sum[Z] / samples;

  // 注意：Z轴偏置 ≈ 9.8 m/s²（如果Z朝上），但我们仍减去它
  // 实际轨迹重建需在Unity中结合姿态（用陀螺仪+加速度计融合）来分离重力！

  Serial.print("Bias Calibrated: X=");
  Serial.print(acc_bias[X], 3);
  Serial.print(", Y=");
  Serial.print(acc_bias[Y], 3);
  Serial.print(", Z=");
  Serial.println(acc_bias[Z], 3);

  calibrated = true;

  // 清空滤波缓冲区
  memset(acc_buffer, 0, sizeof(acc_buffer));
  memset(gyro_buffer, 0, sizeof(gyro_buffer));
  buffer_index = 0;
}

// 应用滑动平均滤波
void applyMovingAverage(float raw_acc[3], float raw_gyro[3], float filtered_acc[3], float filtered_gyro[3]) {
  // 更新缓冲区
  acc_buffer[buffer_index][X] = raw_acc[X];
  acc_buffer[buffer_index][Y] = raw_acc[Y];
  acc_buffer[buffer_index][Z] = raw_acc[Z];

  gyro_buffer[buffer_index][X] = raw_gyro[X];
  gyro_buffer[buffer_index][Y] = raw_gyro[Y];
  gyro_buffer[buffer_index][Z] = raw_gyro[Z];

  // 计算平均值
  for (int axis = 0; axis < 3; axis++) {
    float acc_sum = 0, gyro_sum = 0;
    for (int i = 0; i < FILTER_SIZE; i++) {
      acc_sum += acc_buffer[i][axis];
      gyro_sum += gyro_buffer[i][axis];
    }
    filtered_acc[axis] = acc_sum / FILTER_SIZE;
    filtered_gyro[axis] = gyro_sum / FILTER_SIZE;
  }

  buffer_index = (buffer_index + 1) % FILTER_SIZE;
}

void loop() {
  // 检查串口命令
  if (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    if (cmd.equalsIgnoreCase("c") || cmd.equalsIgnoreCase("calibrate")) {
      performCalibration();
    }
  }

  sensors_event_t a, g, temp;
  mpu.getEvent(&a, &g, &temp);

  // 减去偏置（即使未校准，初始为0也不影响）
  float raw_acc[3] = {
    a.acceleration.x - acc_bias[X],
    a.acceleration.y - acc_bias[Y],
    a.acceleration.z - acc_bias[Z]
  };
  float raw_gyro[3] = { g.gyro.x, g.gyro.y, g.gyro.z };

  // 滑动平均滤波
  float filtered_acc[3], filtered_gyro[3];
  applyMovingAverage(raw_acc, raw_gyro, filtered_acc, filtered_gyro);

  // 输出格式：CSV，便于 Unity 解析
  // 格式: ax,ay,az,gx,gy,gz
  Serial.print(filtered_acc[X], 4);
  Serial.print(",");
  Serial.print(filtered_acc[Y], 4);
  Serial.print(",");
  Serial.print(filtered_acc[Z], 4);
  Serial.print(",");
  Serial.print(filtered_gyro[X], 4);
  Serial.print(",");
  Serial.print(filtered_gyro[Y], 4);
  Serial.print(",");
  Serial.println(filtered_gyro[Z], 4);

  delay(10); // ~100Hz 输出频率（可调）
}