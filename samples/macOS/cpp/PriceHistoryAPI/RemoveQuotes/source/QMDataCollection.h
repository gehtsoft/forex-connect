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

#include "IQMDataCollection.h"
#include "ThreadSafeAddRefImpl.h"

/** Collection of Quotes Manager cache data */
class QMDataCollection : public TThreadSafeAddRefImpl<IQMDataCollection>
{
 public:
    QMDataCollection();
    ~QMDataCollection();

    /** @name IQMDataCollection interface implementation */
    //@{
    virtual void add(IQMData *data);
    virtual void remove(std::size_t index);
    virtual std::size_t size() const;
    virtual IQMData* get(std::size_t index) const;
    //@}

 private:
    std::vector<IQMData *> mElements;
};
