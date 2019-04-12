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

class IQMData;

/** The interface to the collection of Quotes Manager cache data */
class IQMDataCollection : public pricehistorymgr::IAddRef
{
 public:
    /** Adds the data slice to collection
        @param data
            Data for adding
    */
    virtual void add(IQMData *data) = 0;

    /** Removes the data slice by its index.
        @param index
            Index of element
    */
    virtual void remove(std::size_t index) = 0;

    /** Gets the number of data slices in the collection */
    virtual std::size_t size() const = 0;

    /** Gets the data slice by its index.
        @param index
            Index of element
        @return
            Object instance or NULL if index out of range
    */
    virtual IQMData* get(std::size_t index) const = 0;
};
