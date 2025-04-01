#pragma once

// TestStructs.h - Sample C++ structs for size comparison

#include <stdint.h>

// Define a macro for array size
#define ARRAY_SIZE 5

// Define a global constant
const int NAME_LENGTH = 10;

#pragma pack(push, 1)  // Ensure structs are packed without padding

// Simple struct with basic types
struct SimpleStruct 
{
    int32_t id;
    bool flag;
    char letter;
    double value;
};

// Struct with arrays
struct ArrayStruct 
{
    int32_t counts[ARRAY_SIZE];
    char name[NAME_LENGTH];
};

// Nested struct example
struct Point 
{
    int32_t x;
    int32_t y;
};

// A struct with nested structs, 
// and with '{' in the name line
struct Rectangle {
    Point topLeft;
    Point bottomRight;
};

// Complex struct with mixed types
struct ComplexStruct 
{
    int64_t id;
    uint16_t flags;
    double values[3];
    uint8_t type;
};

// A struct with missing pair in the .cs file
struct CppOnlyStruct 
{
    int32_t id;
    bool flag;
    char letter;
    double value;
};

#pragma pack(pop)  // Restore default packing
