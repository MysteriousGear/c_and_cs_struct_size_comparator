#pragma once

// TestStructs.h - Sample C++ structs for size comparison

#include <stdint.h>

#pragma pack(push, 1)  // Ensure structs are packed without padding

// Simple struct with basic types
struct SimpleStruct 
{
    int32_t id;        // 4 bytes
    bool flag;         // 1 byte
    char letter;       // 1 byte
    double value;      // 8 bytes
};                     // Total: 14 bytes

// Struct with arrays
struct ArrayStruct 
{
    int32_t counts[5]; // 5 * 4 = 20 bytes
    char name[10];     // 10 bytes
};                     // Total: 30 bytes

// Nested struct example
struct Point 
{
    int32_t x;         // 4 bytes
    int32_t y;         // 4 bytes
};                     // Total: 8 bytes

// A struct with nested structs, 
// and with '{' in the name line
struct Rectangle {
    Point topLeft;     // 8 bytes
    Point bottomRight; // 8 bytes
};                     // Total: 16 bytes

// Complex struct with mixed types
struct ComplexStruct 
{
    int64_t id;        // 8 bytes
    uint16_t flags;    // 2 bytes
    double values[3];  // 3 * 8 = 24 bytes
    uint8_t type;      // 1 byte
};                     // Total: 35 bytes

// A struct with missing pair in the .cs file
struct CppOnlyStruct 
{
	int32_t id;        // 4 bytes
	bool flag;         // 1 byte
	char letter;       // 1 byte
	double value;      // 8 bytes
};                     // Total: 14 bytes

#pragma pack(pop)  // Restore default packing
