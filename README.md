# BongleMonitor

## Kernel level

- a) Copy the overlay driver module files (e.g., for MHS35 LCD screen, mhs35.dtbo, mhs35-overlay.dtb) into /boot/overlays/ folder from Raspbian¡¯s /boot/overlays/ folder

- b) Create a new folder /boot/overlays.bak/ and copy /boot/overlays/vc4-kms-v3d-pi4.dtbo into it for backup; then copy /boot/overlays/vc4-fkms-v3d.dtbo (/boot/overlays/vc4-fkms-v3d-pi4.dtbo is preferred for Pi 4) into /boot/overlays/vc4-kms-v3d-pi4.dtbo , this is to force Batocera to load the LCD¡¯s 3D driver instead of HDMI¡¯s 3D driver

- c) Adjust settings in /boot/config.txt and /boot/cmdline.txt

- i) In /boot/config.txt , add the LCD driver line dtoverlay=mhs35:rotate=90 ; add the GPU 3D acceleration line dtoverlay=vc4-fkms-v3d,noaudio ; you can optionally add the display resolution lines, you can use either fixed resolution (e.g., 800¡Á480):
```
   hdmi_group=2
   hdmi_mode=87
   hdmi_cvt 800 480 60 6 0 0 0
   hdmi_drive=2
```
- Or flexible resolution (with initial resolution, e.g., 85 for 1280¡Á720, please refer to Raspberry Pi official link):
```
   hdmi_group=2
   hdmi_mode=85
   hdmi_drive=2
```
- ii) In /boot/cmdline.txt , append console=serial0,115200 fbcon=map:10 fbcon=font:ProFont6x11 to the line (don¡¯t create a new line), these optional settings are for console display