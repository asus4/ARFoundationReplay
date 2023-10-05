#!/bin/sh -xe

# Build Avfi macOS plugin

CFLAGS="-O2 -Wall"

LIBS="-framework Foundation -framework AVFoundation"
LIBS+=" -framework CoreMedia -framework CoreVideo -framework QuartzCore"

MAC_ARGS="-shared -rdynamic -fPIC -fobjc-arc"

SRC="../com.github.asus4.arfoundationreplay/Runtime/Plugins/iOS"
DST="../com.github.asus4.arfoundationreplay/Runtime/Plugins/macOS"

rm -f *.so *.bundle

gcc -target x86_64-apple-macos10.13 $CFLAGS $MAC_ARGS $SRC/Avfi.m $SRC/AvfiMetaPlayer.m $LIBS -o x86_64.so
gcc -target  arm64-apple-macos10.13 $CFLAGS $MAC_ARGS $SRC/Avfi.m $SRC/AvfiMetaPlayer.m $LIBS -o arm64.so

lipo -create -output Avfi.bundle x86_64.so arm64.so

cp Avfi.bundle $DST
