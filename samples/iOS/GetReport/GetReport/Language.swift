import Foundation

enum Language {
    case enu
    case jpn
    case fra
    case esp
    case cht
    case chs
    case rus
    
    static func toDescription(language: Language) -> String {
        switch language {
        case .enu:
            return "English"
        case .jpn:
            return "Japanese"
        case .fra:
            return "French"
        case .esp:
            return "Spanish"
        case .cht:
            return "Chinese Traditional"
        case .chs:
            return "Chinese Simplified"
        case .rus:
            return "Russian"
        }
    }
    
    static func toString(language: Language) -> String {
        switch language {
        case .enu:
            return "enu"
        case .jpn:
            return "jpn"
        case .fra:
            return "fra"
        case .esp:
            return "esp"
        case .cht:
            return "cht"
        case .chs:
            return "chs"
        case .rus:
            return "rus"
        }
    }
    
    static func fromDescription(language: String) -> Language? {
        switch language {
        case "English":
            return .enu
        case "Japanese":
            return .jpn
        case "French":
            return .fra
        case "Spanish":
            return .esp
        case "Chinese Traditional":
            return .cht
        case "Chinese Simplified":
            return .chs
        case "Russian":
            return .rus
        default:
            return nil
        }
    }
    
    static func allDescriptions() -> [String] {
        return [  toDescription(language: enu),
                  toDescription(language: jpn),
                  toDescription(language: fra),
                  toDescription(language: esp),
                  toDescription(language: cht),
                  toDescription(language: chs),
                  toDescription(language: rus)
        ]
    }

}
