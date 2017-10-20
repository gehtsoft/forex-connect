#!/bin/bash

CONFIG=

case "$1" in
  "") CONFIG=Release;;
  "Release") CONFIG=Release;;
  "Debug") CONFIG=Debug;;
esac

echo Configuration=$CONFIG

dotnet restore
dotnet build CreateOTOCO.csproj /p:Configuration=$CONFIG

