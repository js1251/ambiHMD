// NEOPIXEL BEST PRACTICES for most reliable operation:
// - Add 1000 uF CAPACITOR between NeoPixel strip's + and - connections.
// - MINIMIZE WIRING LENGTH between microcontroller board and first pixel.
// - NeoPixel strip's DATA-IN should pass through a 300-500 OHM RESISTOR.
// - AVOID connecting NeoPixels on a LIVE CIRCUIT. If you must, ALWAYS
//   connect GROUND (-) first, then +, then data.
// - When using a 3.3V microcontroller with a 5V-powered NeoPixel strip,
//   a LOGIC-LEVEL CONVERTER on the data line is STRONGLY RECOMMENDED.
// (Skipping these may work OK on your workbench but can fail in the field)

#include <Adafruit_NeoPixel.h>
#ifdef __AVR__
 #include <avr/power.h> // Required for 16 MHz Adafruit Trinket
#endif

// Which pin is connected to the LED strips on each side
#define LED_PIN_LEFT 6
#define LED_PIN_RIGHT 7

// How many NeoPixels per eye
#define LED_COUNT_PER_EYE 8

// communication protocol
#define TERMINATOR_CHAR '\n'

// BRIGHTNESS(000-255) + 2 * LED_COUNT_PER_EYE * |R|G|B| (000-255) = 
#define MAX_MESSAGE_LENGTH 147 // 3 + 2 * LED_COUNT_PER_EYE * (3 * 3);

Adafruit_NeoPixel strip_left(LED_COUNT_PER_EYE, LED_PIN_LEFT, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel strip_right(LED_COUNT_PER_EYE, LED_PIN_RIGHT, NEO_GRB + NEO_KHZ800);


// runs once at startup --------------------------------
void setup() {
  // These lines are specifically to support the Adafruit Trinket 5V 16 MHz.
  // Any other board, you can remove this part (but no harm leaving it):
#if defined(__AVR_ATtiny85__) && (F_CPU == 16000000)
  clock_prescale_set(clock_div_1);
#endif
  // END of Trinket-specific code.

  strip_left.begin();
  strip_right.begin();

  strip_left.setBrightness(20);
  strip_right.setBrightness(20);

  strip_left.setPixelColor(0, strip_left.Color(0, 255, 0));
  strip_right.setPixelColor(0, strip_right.Color(255, 0, 0));

  strip_left.show();
  strip_right.show();

  // serial connection
  Serial.begin(115200);
}

// here to process incoming serial data after a terminator received
void process_data(const char * data) {
  static char byte_value[4];

  // get the led brightness
  byte_value[0] = data[0];
  byte_value[1] = data[1];
  byte_value[2] = data[2];
  byte_value[3] = '\0';

  int brightness = atoi(byte_value);

  strip_left.setBrightness(brightness);
  strip_right.setBrightness(brightness);

  int offset = 3;
  for (int i = 0; i < LED_COUNT_PER_EYE; i++) {
    byte_value[0] = data[offset + (i * 9) + 0];
    byte_value[1] = data[offset + (i * 9) + 1];
    byte_value[2] = data[offset + (i * 9) + 2];
    byte_value[3] = '\0';

    int r = atoi(byte_value);
    byte_value[0] = data[offset + (i * 9) + 3];
    byte_value[1] = data[offset + (i * 9) + 4];
    byte_value[2] = data[offset + (i * 9) + 5];
    byte_value[3] = '\0';

    int g = atoi(byte_value);
    byte_value[0] = data[offset + (i * 9) + 6];
    byte_value[1] = data[offset + (i * 9) + 7];
    byte_value[2] = data[offset + (i * 9) + 8];
    byte_value[3] = '\0';

    int b = atoi(byte_value);
    strip_left.setPixelColor(i, strip_left.Color(r, g, b));
  }

  offset += LED_COUNT_PER_EYE * 3 * 3;
  for (int i = 0; i < LED_COUNT_PER_EYE; i++) {
    byte_value[0] = data[offset + (i * 9) + 0];
    byte_value[1] = data[offset + (i * 9) + 1];
    byte_value[2] = data[offset + (i * 9) + 2];
    byte_value[3] = '\0';

    int r = atoi(byte_value);
    byte_value[0] = data[offset + (i * 9) + 3];
    byte_value[1] = data[offset + (i * 9) + 4];
    byte_value[2] = data[offset + (i * 9) + 5];
    byte_value[3] = '\0';

    int g = atoi(byte_value);
    byte_value[0] = data[offset + (i * 9) + 6];
    byte_value[1] = data[offset + (i * 9) + 7];
    byte_value[2] = data[offset + (i * 9) + 8];
    byte_value[3] = '\0';

    int b = atoi(byte_value);
    strip_right.setPixelColor(i, strip_right.Color(r, g, b));
  }

  strip_left.show();
  strip_right.show();
}

void processIncomingByte(const byte inByte) {
  static char input_line[MAX_MESSAGE_LENGTH];
  static unsigned int input_pos = 0;

  switch (inByte) {
    case '\n':   // end of text
      input_line[MAX_MESSAGE_LENGTH] = 0;  // terminating null byte

      // terminator reached! process input_line here ...
      process_data(input_line);

      // reset buffer for next time
      input_pos = 0;  
      break;
    case '\r': // discard carriage return
      break;

    default:
      // keep adding if not full ... allow for terminating null byte
      if (input_pos < (MAX_MESSAGE_LENGTH - 1)) {
        input_line[input_pos++] = inByte;
      }
      break;
  }
}

void loop() {
  // if serial data available, process it
  while (Serial.available () > 0) {
    processIncomingByte(Serial.read());
  }
}