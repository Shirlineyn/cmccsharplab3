#pragma once

#include "pch.h"
#include <time.h>
#include "mkl.h" 

extern "C" _declspec(dllexport)
int VM_Tan(MKL_INT n, double* x, double* y_HA, double* y_EP, double& EPtoHA);