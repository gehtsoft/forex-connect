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
package removequotes;

/** The interface to the Quotes Manager Storage data */
public interface IQMData {

    /**
     * Gets the name of the instrument
     */
    String getInstrument();

    /**
     * Gets the name of the timeframe in which the data are stored in cache
     */
    String getTimeframe();

    /**
     * Gets the year to which the data belongs to
     */
    int getYear();

    /**
     * Gets the size of the data in bytes
     */
    long getSize();
}
