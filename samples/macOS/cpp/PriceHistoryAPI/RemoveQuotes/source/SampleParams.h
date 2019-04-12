/* Copyright 2019 FXCM Global Services, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use these files except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
#pragma once

/** Class to work with sample parameters. */
class SampleParams
{
 public:
    class Strings
    {
    public:
        static const char *instrumentNotSpecified;
        static const char *yearNotSpecified;
    };

 public:
    SampleParams(int, char **);
    ~SampleParams(void);

    const char *getInstrument();
    int getYear();


 private:
    const char *getArgument(int, char **, const char *);

    std::string mInstrument;
    int mYear;
};

