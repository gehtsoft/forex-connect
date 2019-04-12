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
package getliveprices.pricedata;

/**
 * Interface to the collections of the offer
 */
public interface IOfferCollection extends Iterable<IOffer> {
    /**
     * Get number of the offers in the collection
     */
    int size();

    /**
     * Gets the offer by its index @param index The index of the offer (0 is
     * first)
     * 
     * @param index
     */
    IOffer get(int index);
}
