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
  //Serial.begin(9600);
}

void process_data(const byte * data) {
  unsigned int brightness = data[0];
  strip_left.setBrightness(brightness);
  strip_right.setBrightness(brightness);

  for (int i = 0; i < 2; i++) {
    for (int j = 0; j < LED_COUNT_PER_EYE; j++) {
      // offset + 1 since first byte is brightness
      unsigned int offset = (i * LED_COUNT_PER_EYE + j) * 3 + 1;
      
      unsigned int r = data[offset];
      unsigned int g = data[offset + 1];
      unsigned int b = data[offset + 2];

      if (i == 0) {
        strip_left.setPixelColor(j, strip_left.Color(r, g, b));
      } else {
        strip_right.setPixelColor(j, strip_right.Color(r, g, b));
      }
    }
  }

  strip_left.show();
  strip_right.show();
}

void process_incoming(const byte inByte) {
  static const unsigned int max_buffer_length = 1 + LED_COUNT_PER_EYE * 2 * 3;
  static byte buffer[max_buffer_length];
  static unsigned int buffer_pos;

  // terminator reached
  if (inByte == 255) {
    process_data(buffer);
    buffer_pos = 0;
    return;
  }

  // invalid message
  if (buffer_pos >= max_buffer_length) {
    buffer_pos = 0;
  }

  // add byte to buffer
  buffer[buffer_pos] = inByte;
  buffer_pos++;
}

void loop() {
  while (Serial.available() > 0) {
    process_incoming(Serial.read());
  }
}