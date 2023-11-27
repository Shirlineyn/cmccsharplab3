#pragma once

#include "pch.h"
#include "mkl.h"

extern "C" __declspec(dllexport) int Lab3CubicSpline(int nX, const double* X,
	const double* Y, int nCalcX, const double* calcX, double* splineValues);

extern "C" __declspec(dllexport) int Lab3OptimizeSpline(int nX, double* X, const double* Y,
	int M, int* StopCondition, double* splineBaseXValues, double* splineValues, double* splineError,
	int* error, int MaxIterations);
