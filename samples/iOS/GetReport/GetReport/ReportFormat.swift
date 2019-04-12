import Foundation

enum ReportFormat {
    case HTML
    case XML
    case PDF
    case XLS
    
    static func toDescription(format: ReportFormat) -> String {
        switch format {
        case .HTML:
            return "HTML format"
        case .XML:
            return "XML format"
        case .PDF:
            return "Adobe PDF format"
        case .XLS:
            return "Microsoft XLS format"
        }
    }
    
    static func allDescriptions() -> [String] {
        return [
            ReportFormat.toDescription(format: .HTML),
            ReportFormat.toDescription(format: .PDF),
            ReportFormat.toDescription(format: .XML),
            ReportFormat.toDescription(format: .XLS)
        ]
    }
    
    static func toString(format: ReportFormat) -> String {
        switch format {
        case .HTML:
            return "HTML"
        case .XML:
            return "XML"
        case .PDF:
            return "PDF"
        case .XLS:
            return "XLS"
        }
    }
    
    static func fromDescription(format: String) -> ReportFormat? {
        switch format {
        case "HTML format":
            return .HTML
        case "XML format":
            return .XML
        case "Adobe PDF format":
            return .PDF
        case "Microsoft XLS format":
            return .XLS
        default:
            return nil
        }
    }
    
}

