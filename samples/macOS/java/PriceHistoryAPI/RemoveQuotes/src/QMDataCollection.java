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

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;

/**
 * The implementation of the collection of quotes manager data
 */
class QMDataCollection implements IQMDataCollection {

    /**
     * The storage for data blocks
     */
    private List<IQMData> mList = new ArrayList<IQMData>();

    /**
     * Adds a new data descriptor in the collection
     * 
     * @param data
     */
    public void add(QMData data) {
        mList.add(data);
    }

    @Override
    public Iterator<IQMData> iterator() {
        return mList.iterator();
    }

    @Override
    public int size() {
        return mList.size();
    }

    @Override
    public IQMData get(int index) {
        return mList.get(index);
    }
}