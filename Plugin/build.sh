#!/bin/sh

CFLAGS="-O2 -Wall"

LIBS="-framework Foundation -framework AVFoundation"
LIBS+=" -framework CoreMedia -framework CoreVideo -framework QuartzCore"

MAC_ARGS="-shared -rdynamic -fPIC -fobjc-arc"

IOS_ROOT=`xcrun --sdk iphoneos --show-sdk-path`
IOS_ARGS="--sysroot $IOS_ROOT -isysroot $IOS_ROOT -fembed-bitcode -fobjc-arc"

rm *.o *.so *.a *.bundle

set -x

gcc -target x86_64-apple-macos10.13 $CFLAGS $MAC_ARGS Avfi.m AvfiMetaPlayer.m $LIBS -o x86_64.so
gcc -target  arm64-apple-macos10.13 $CFLAGS $MAC_ARGS Avfi.m AvfiMetaPlayer.m $LIBS -o arm64.so

gcc $CFLAGS $IOS_ARGS -c Avfi.m AvfiMetaPlayer.m

lipo -create -output Avfi.bundle x86_64.so arm64.so

ar -crv libAvfi.a Avfi.o AvfiMetaPlayer.o

DST="../Packages/com.github.asus4.ARRecorder/Plugins"
cp Avfi.bundle $DST
cp libAvfi.a $DST
