byte command = 0;
enum state {commandReadMode, dataReadMode};

enum state currentState = commandReadMode;


char currentDataBuffer[4800];
size_t numBytesRead = 0;

#define SCK 2
#define DATA_OUT 3
#define ENABLE 4 // active low
#define MODE0 5
#define MODE1 6
#define MODE2 7







//set data lines, shifting out MSB first. (bit 31)
void shiftDataOut(const char *buffer, int offset)
{

  //set enable low
  digitalWrite(ENABLE, LOW);
  //PORTG &= ~(1 << 5);


  const byte b0 = buffer[0 + offset], b1 = buffer[1 + offset], b2 = buffer[2 + offset], b3 = buffer[3 + offset];
  byte nextComponent[4] = {b0, b1, b2, b3};
  int data;
  //loop 4 bytes
  for (int byte_index = 0; byte_index < 4; byte_index += 1 ) {
    // loop 8 bits = 32 bit writes

    for (int bit_index = 7; bit_index >= 0; bit_index -= 1)
    {
      //clock low
      //PORTE &= ~(1 << 4);
      digitalWrite(SCK, LOW);
      data = nextComponent[byte_index];
      int bitState = bitRead(data, bit_index);
      
      //magical xor bit set to 0 or 1.
      //PORTE ^= (-bitState ^ PORTE) & (1 << 5);

      digitalWrite(DATA_OUT, bitState);

      //clock high
      //PORTE |= (1 << 4);
      digitalWrite(SCK, HIGH);
    }
  }



  //end message



  //PORTE &= ~(1 << 5);
  //PORTE &= ~(1 << 4);
  digitalWrite(DATA_OUT, LOW);
  digitalWrite(SCK, LOW);
  //PORTG |= (1 << 5);
  digitalWrite(ENABLE, HIGH);
}



unsigned long extract_long(const byte *buffer, int offset)
{
  const byte b0 = buffer[0 + offset], b1 = buffer[1 + offset], b2 = buffer[2 + offset], b3 = buffer[3 + offset];
  unsigned long returnNum = ( (unsigned long)b0 << 24) | ((unsigned long)b1 << 16) | ((unsigned long)b2 << 8) | ((unsigned long)b3);
  /*
    Serial.print("extracting number : ");

    Serial.print(0+offset);
    Serial.print(" : ");
    Serial.print(b0,BIN);
    Serial.print(" : ");

    Serial.print(1+offset);
    Serial.print(" : ");
    Serial.print(b1,BIN);
    Serial.print(" : ");

    Serial.print(2+offset);
    Serial.print(" : ");
    Serial.print(b2,BIN);
    Serial.print(" : ");

    Serial.print(3+offset);
    Serial.print(" : ");
    Serial.print(b3,BIN);
    Serial.print(" : ");

    Serial.println(returnNum,BIN);
  */
  return returnNum;
}

void setup()
{

  //MODE and write pins
  pinMode(MODE0, OUTPUT);
  pinMode(MODE1, OUTPUT);
  pinMode(MODE2, OUTPUT);
  //MOSI
  pinMode(DATA_OUT, OUTPUT);
  pinMode(SCK, OUTPUT);
  pinMode(ENABLE, OUTPUT);

  //start all control lines low
  digitalWrite(MODE0, LOW);
  digitalWrite(MODE1, LOW);
  digitalWrite(MODE2, LOW);

  digitalWrite(DATA_OUT, LOW);
  digitalWrite(SCK, LOW);
  //active low - so start high.
  digitalWrite(ENABLE, HIGH);

  Serial.begin(230400);
}

void loop()
{

  //if we're in command mode - then
  //look for single byte commands.
  if (currentState == commandReadMode) {

    if (Serial.available() > 0)
    {
      command = Serial.read();
      if (command == 0)
      {
          //start all control lines low
          digitalWrite(MODE0, LOW);
          digitalWrite(MODE1, LOW);
          digitalWrite(MODE2, LOW);
        Serial.println("entering vertex data mode");
        currentState = dataReadMode;
      }

      if(command == 3){
        Serial.println("entering frameBuffer blank mode");
        digitalWrite(MODE0, LOW);
        digitalWrite(MODE1, HIGH);
        digitalWrite(MODE2, HIGH);
      }
    }
  }

  //if we're in dataReadMode then
  //look for lots of data and push it into a buffer
  else if (currentState == dataReadMode) {

    if (Serial.available() > 0)
    {
      numBytesRead = Serial.readBytes(currentDataBuffer, 4800);

      //chop the list into 12 byte chunks.
      for (int i = 0; i < numBytesRead; i = i + 12) {

        //grab the next 32 bits and shift it out using sync serial. (SPI)
        shiftDataOut(currentDataBuffer, 0 + i);

        shiftDataOut(currentDataBuffer, 4 + i);

        shiftDataOut(currentDataBuffer, 8 + i);

      }

      Serial.println("entering command mode.");
      Serial.flush();
      currentState = commandReadMode;
    }
  }

}
