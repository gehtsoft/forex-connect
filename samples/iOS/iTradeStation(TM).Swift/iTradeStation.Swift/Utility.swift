import Foundation
import UIKit

extension Float {
    func parseToPlaces(places:Int) -> String {
        let formatStr = String(format: "%%.%df", places);
        return String(format: formatStr, self)
    }
}

extension Double {
    func parseToPlaces(places:Int) -> String {
        let formatStr = String(format: "%%.%df", places);
        return String(format: formatStr, self)
    }
}
