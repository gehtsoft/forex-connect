#!/bin/bash

export LD_LIBRARY_PATH=`pwd`/bin/netcoreapp1.0
dotnet run --project NetStopLimit.csproj $@