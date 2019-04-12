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
#include "stdafx.h"
#include "QMDataCollection.h"
#include "IQMData.h"

QMDataCollection::QMDataCollection()
    : mElements()
{
}

QMDataCollection::~QMDataCollection()
{
    std::for_each(mElements.begin(), mElements.end(), std::mem_fun(&IQMData::release));
}

void QMDataCollection::add(IQMData *data)
{
    if (NULL == data)
    {
        return;
    }

    data->addRef();
    mElements.push_back(data);
}

void QMDataCollection::remove(std::size_t index)
{
    if (index >= mElements.size())
    {
        return;
    }

    IQMData *data(mElements[index]);
    data->release();
    mElements.erase(mElements.begin() + index);
}

std::size_t QMDataCollection::size() const
{
    return mElements.size();
}

IQMData* QMDataCollection::get(std::size_t index) const
{
    if (index >= mElements.size())
    {
        return NULL;
    }

    IQMData *data(mElements[index]);
    data->addRef();
    return data;
}
