# simpleGPUController
control software for simpleGPU

---
### what is in here:

This repo contains a few things to test the simpleGPU:
* A .dotnet core command line application. The command line application in the `gpuTest` 
directory is capable sending commands and data to an arduino over usb/serial.
* Some Arduino test firmware. The arduino/microcontroller running this firmware sends data over 
bit-banged SPI to the simpleGPU FPGA board.
* a .dylib file `libarduinoComsTest1.dylib` which implements some basic serial port communication on mac in `C`.
* a test .obj file - `/gpuTests/teapot.obj` which contains the vert and triangle information for the famous teapot model.
* a `testMVP.txt` file - this is the model view projection matrix which 
is used to project the verts into pespective camera space. The FPGA expects each number to be represented in
[Q16.16](https://en.wikipedia.org/wiki/Q_(number_format)) format. The matrix components are flattened.
* a `testVectors.txt` file which contains all the verts we want to draw. 
The FPGA expects each number to be represented in [Q16.16](https://en.wikipedia.org/wiki/Q_(number_format)) format.
 this is organized as follows: 
```
x
y
z
x
y
z
```

*details:*

---
### command line app
* Very rough - has a bunch of command line modes which just run different top level functions - can be used to:
  * `testFixedPoint` generate output vertex and matrix data from a given .obj file.
  *  `testnativeinterop` send commands over serial to a microcontroller running the test firmware.
  * ` ` generate an image from an .obj
  * `testMatrixMultiplyOrder` sanity checks.

---
### test firmware
* Watches for commands and data on the serial port (UART) of the microcontroller and transmits these over bit-banged SPI. It also 
sets some mode output pins that the FPGA expects to be set which control different write modes the simpleGPU supports. See the docs in
the simpleGPU FPGA repo for more info. There is a teensy version of this firmware that runs much faster (tested on teensy 3.2 mcu) 
because the CPU is faster, but also because the teensy supports usb full speed over serial.
* commands the firmware supports:
  * 1 byte `0` - `go into vertex data mode` - expecting verts which it will transmit only when buffer is filled 
  - set to 4800 bytes currently - 32 bits per component * 3 per vert = 12bytes per vert = 4800/12 = 400 verts.
  * 1 byte `3` `go into scren blank mode` - will send a command to FPGA to blank the entire frame buffer. (clear screen)
  * 
---
### serialCommunication dylib
please see serial repo for info.


  
