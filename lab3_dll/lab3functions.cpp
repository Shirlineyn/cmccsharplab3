#include "pch.h"
#include <iostream>
#include <time.h>
#include <math.h>
#include "mkl.h"
#include "lab3functons.h"

struct params
{
	const double* uniformX = NULL;
	const double* X = NULL;
	const double* Y = NULL;
};

/*
int nX - число узлов сплайна
const double* X - массив узлов сплайна
const double* Y - массив заданных значений векторной функции
int nCalcX - число узлов для вычисления значения сплайна
const double* calcX - сетка для вычисления значений сплайна
double* splineValues - массив вычисленных значений сплайна
*/
extern "C" __declspec(dllexport) int Lab3CubicSpline(int nX, const double* X, 
	const double* Y, int nCalcX, const double* calcX, double* splineValues) 
{
	MKL_INT s_order = DF_PP_CUBIC; 
	MKL_INT s_type = DF_PP_NATURAL;

	MKL_INT bc_type = DF_BC_2ND_LEFT_DER | DF_BC_2ND_RIGHT_DER;

	int nY = 1;
	const double d2L = 0.0;
	const double d2R = 0.0;
	const int nS = nX;

	const double sL = X[0];
	const double sR = X[nX - 1];

	double* scoeff = new double[nY * (nX - 1) * s_order];
	try
	{
		DFTaskPtr task;
		int status = -1;
		status = dfdNewTask1D(&task, nX, X, DF_NON_UNIFORM_PARTITION, nY, Y, DF_NO_HINT); 

		if (status != DF_STATUS_OK) throw 1;

		double bc[2]{ d2L, d2R };
		status = dfdEditPPSpline1D(task, s_order, s_type, bc_type, bc, DF_NO_IC, NULL, scoeff, DF_NO_HINT);
		if (status != DF_STATUS_OK) throw 2;

		status = dfdConstruct1D(task, DF_PP_SPLINE, DF_METHOD_STD);
		if (status != DF_STATUS_OK) throw 3;

		int nDorder = 1;
		MKL_INT dorder[] = { 1 };
		status = dfdInterpolate1D(task, DF_INTERP, DF_METHOD_PP, nCalcX, calcX, DF_UNIFORM_PARTITION, nDorder,
			dorder, NULL, splineValues, DF_NO_HINT, NULL); 
		if (status != DF_STATUS_OK) throw 4;

		status = dfDeleteTask(&task);
		if (status != DF_STATUS_OK) throw 5;
	}
	catch (int ret)
	{
		delete[] scoeff;
		return ret;
	}
	delete[] scoeff;
	return 0;
}

/*
MKL_INT* ntrueX - размер изначальной сетки N
MKL_INT* nX - размер равномерной сетки M
double* Y - значения на сетке размера M == nX - ПОДБИРАЕМЫЙ ПАРАМЕТР
double* YError - ошибка полученного сплайна размера N
params* param - указатель на структуру с дополнительными параметрами:
param->uniformX : const double* X - равномерная сетка размера M == nX
params->X : const double* Xtrue - изначальная сетка размера N
params->Y : const double* Ytrue - значения на изначальной сетке размера N
*/
void SplineError(MKL_INT* ntrueX, MKL_INT* nX, double* Y, double* YError, void* param)
{
	params* p = (params*)param;
	Lab3CubicSpline(*nX, p->uniformX, Y, *ntrueX, p->X, YError);

	for (int i = 0; i < *ntrueX; i++)
	{
		YError[i] -= p->Y[i];
	}
}

/*
int nX - число узлов изначальной функции
(const) double* X - координаты узлов
const double* Y - значения изначальной функции
int M - число узлов сглаживающего сплайна
int* StopCondition - причина остановки алгоритма оптимизации
double* splineBaseXValues -значение оптимального сплайна в изначальных точках
double* splineValues - значения оптимального сплайна на равномерной сетке
double* splineError - квадрат невязки оптимального сплайна на изначальной сетке
int* error - код ошибки
int MaxIterations - максимальное к-во итераций
*/
extern "C" __declspec(dllexport) int Lab3OptimizeSpline(int nX, double* X, const double* Y,
	int M, int* StopCondition, double* splineBaseXValues, double* splineValues, double* splineError,
	int* error, int MaxIterations)
{
	// константы
	MKL_INT niter1 = MaxIterations;
	MKL_INT niter2 = MaxIterations / 10;
	MKL_INT ndone_iter = 0;
	double rs = 10;
	int nY = 1;
	const double eps[6] =
		{1.0E-12, 1.0E-12, 1.0E-12, 1.0E-12, 1.0E-12, 1.0E-12};
	double jac_eps = 1.0E-8; 

	_TRNSP_HANDLE_t handle = NULL;

	double* uniformX = NULL;
	double* approxY = NULL;
	double* YError = NULL;
	double* YErrorJac = NULL;
	params* param = NULL;
	int* checkInfo = new int();
	int* iterations_N = new int();
	double* startError = NULL;

	try
	{
		// равномерная сетка
		uniformX = new double[M];
		double step = (X[nX - 1] - X[0]) / M;
		for (int i = 0; i < M - 1; i++)
		{
			uniformX[i] = X[0] + step * i;
		}
		uniformX[M - 1] = X[nX - 1];

		//подготавливаем параметры
		param = new params();
		param->uniformX = uniformX;
		param->X = X;
		param->Y = Y;

		// начальное значение приближенных коэффициентов сетки для оптимизации
		approxY = new double[M];
		for (int i = 0; i < M; i++)
		{
			approxY[i] = 1.0;
		}

		YError = new double[nX];
		YErrorJac = new double[nX * M];

		//инициализация задачи
		MKL_INT ret = dtrnlsp_init(&handle, &M, &nX, approxY, eps, &niter1, &niter2, &rs);
		if (ret != TR_SUCCESS) throw 1;

		// Проверка корректности входных данных
		ret = dtrnlsp_check(&handle, &M, &nX, YErrorJac, YError, eps, checkInfo);
		if (ret != TR_SUCCESS) throw 2;

		MKL_INT RCI_Request = 0;
		while (true)
		{
			ret = dtrnlsp_solve(&handle, YError, YErrorJac, &RCI_Request);
			if (ret != TR_SUCCESS) throw 3;

			if (RCI_Request == 0) continue;
			else if (RCI_Request == 1) SplineError(&nX, &M, approxY, YError, param);
			else if (RCI_Request == 2)
			{
				ret = djacobix(SplineError, &M, &nX, YErrorJac, X, &jac_eps, param);
				if (ret != TR_SUCCESS) throw 4;
			}
			else if (RCI_Request >= -6 && RCI_Request <= -1) break;
			else throw 5;
		}
		startError = new double();
		// Завершение итерационного процесса
		ret = dtrnlsp_get(&handle, iterations_N, StopCondition,
			startError, splineError);
		if (ret != TR_SUCCESS) throw 6;;

		// Освобождение ресурсов
		ret = dtrnlsp_delete(&handle);
		if (ret != TR_SUCCESS) throw 7;

		Lab3CubicSpline(M, uniformX, approxY, nX, X, splineBaseXValues);

	}
	catch (int e) { *error = e; }

	if (uniformX != NULL) delete[] uniformX;
	if (YError != NULL) delete[] YError;
	if (approxY != NULL) delete[] approxY;
	if (param != NULL) delete param;
	if (iterations_N != NULL) delete iterations_N;
	if (startError != NULL) delete startError;

	return 0;
}
