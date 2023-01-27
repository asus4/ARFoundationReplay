#!/bin/sh -xe

# Build XR macOS plugin

echo "Building XR macOS plugin..."

CFLAGS="-O2 -Wall"
MAC_ARGS="-shared -rdynamic -fPIC -fobjc-arc"
HEADERS="-IHeaders"
FILES="Source/entry.cpp"

DST="../macOS"

rm -f *.so *.bundle

g++ -target x86_64-apple-macos10.13 $CFLAGS $MAC_ARGS $HEADERS $FILES -o x86_64.so
g++ -target  arm64-apple-macos10.13 $CFLAGS $MAC_ARGS $HEADERS $FILES -o arm64.so

lipo -create -output ARFoundationReplayPlugin.bundle x86_64.so arm64.so

cp ARFoundationReplayPlugin.bundle $DST

rm -f *.so *.bundle

echo "Done."
