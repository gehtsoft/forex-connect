import Foundation
import UIKit

class OffersTableCell : UITableViewCell {
    var bidLabel: UILabel?
    var askLabel: UILabel?
    var commOpenLabel: UILabel?
    var commCloseLabel: UILabel?
    var commTotalLabel: UILabel?
    
    var isCommEnabled: Bool?
    
    func setDataContent(instr: String, bid: Double, ask: Double, bidDirection: Int, askDirection: Int, digits: Int, commOpen: Double, commClose: Double, commTotal: Double) {
        
        textLabel!.text = instr
        bidLabel!.text = bid.parseToPlaces(places: digits)
        askLabel!.text = ask.parseToPlaces(places: digits)
        
        if isCommEnabled! {
            commOpenLabel!.text = commOpen.parseToPlaces(places: 2)
            commCloseLabel!.text = commClose.parseToPlaces(places: 2)
            commTotalLabel!.text = commTotal.parseToPlaces(places: 2)
        }
        
        if  bidDirection > 0 {
            bidLabel!.textColor = UIColor.red
        }
        else if bidDirection < 0 {
            bidLabel!.textColor = UIColor.blue
        }
        else {
            bidLabel!.textColor = UIColor.black
        }
        
        if  askDirection > 0 {
            askLabel!.textColor = UIColor.red
        }
        else if askDirection < 0 {
            askLabel!.textColor = UIColor.blue
        }
        else {
            askLabel!.textColor = UIColor.black
        }
    }
}

class OffersTableCellCommEnabled : OffersTableCell {
    
    override init(style: UITableViewCellStyle, reuseIdentifier: String?) {
        super.init(style: style, reuseIdentifier: reuseIdentifier)
        
        isCommEnabled = true
        
        textLabel!.font = UIFont.systemFont(ofSize: 11.0)
        textLabel!.textColor = UIColor.black
        
        bidLabel = UILabel(frame: CGRect(x: 70.0, y: 10.0, width: 60.0, height: 25.0))
        bidLabel!.tag = 2
        bidLabel!.textAlignment = NSTextAlignment.right
        bidLabel!.font = UIFont.systemFont(ofSize: 11.0)
        bidLabel!.textColor = UIColor.black
        bidLabel!.autoresizingMask = UIViewAutoresizing.flexibleHeight
        contentView.addSubview(bidLabel!)
        
        askLabel = UILabel(frame: CGRect(x: 130, y: 10.0, width: 60.0, height: 25.0))
        askLabel!.tag = 3
        askLabel!.textAlignment = NSTextAlignment.right
        askLabel!.font = UIFont.systemFont(ofSize: 11.0)
        askLabel!.textColor = UIColor.black
        askLabel!.autoresizingMask = UIViewAutoresizing.flexibleHeight
        contentView.addSubview(askLabel!)
        
        commOpenLabel = UILabel(frame: CGRect(x: 190, y: 10.0, width: 50.0, height: 25.0))
        commOpenLabel!.tag = 4
        commOpenLabel!.textAlignment = NSTextAlignment.right
        commOpenLabel!.font = UIFont.systemFont(ofSize: 11.0)
        commOpenLabel!.textColor = UIColor.black
        commOpenLabel!.autoresizingMask = UIViewAutoresizing.flexibleHeight
        contentView.addSubview(commOpenLabel!)
        
        commCloseLabel = UILabel(frame: CGRect(x: 240, y: 10.0, width: 50.0, height: 25.0))
        commCloseLabel!.tag = 5
        commCloseLabel!.textAlignment = NSTextAlignment.right
        commCloseLabel!.font = UIFont.systemFont(ofSize: 11.0)
        commCloseLabel!.textColor = UIColor.black
        commCloseLabel!.autoresizingMask = UIViewAutoresizing.flexibleHeight
        contentView.addSubview(commCloseLabel!)
        
        commTotalLabel = UILabel(frame: CGRect(x: 290, y: 10.0, width: 50.0, height: 25.0))
        commTotalLabel!.tag = 6
        commTotalLabel!.textAlignment = NSTextAlignment.right
        commTotalLabel!.font = UIFont.systemFont(ofSize: 11.0)
        commTotalLabel!.textColor = UIColor.black
        commTotalLabel!.autoresizingMask = UIViewAutoresizing.flexibleHeight
        contentView.addSubview(commTotalLabel!)
        
        accessoryType = .disclosureIndicator
    }
    
    required init?(coder aDecoder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }
}

class OffersTableCellCommDisabled : OffersTableCell {
    
    override init(style: UITableViewCellStyle, reuseIdentifier: String?) {
        super.init(style: style, reuseIdentifier: reuseIdentifier)
        
        isCommEnabled = false
        
        textLabel!.font = UIFont.systemFont(ofSize: 16.0)
        textLabel!.textColor = UIColor.black
        
        bidLabel = UILabel(frame: CGRect(x: 100.0, y: 10.0, width: 80.0, height: 25.0))
        bidLabel!.tag = 2
        bidLabel!.textAlignment = NSTextAlignment.right
        bidLabel!.font = UIFont.systemFont(ofSize: 16.0)
        bidLabel!.textColor = UIColor.black
        bidLabel!.autoresizingMask = UIViewAutoresizing.flexibleHeight
        contentView.addSubview(bidLabel!)
        
        askLabel = UILabel(frame: CGRect(x: 190.0, y: 10.0, width: 80.0, height: 25.0))
        askLabel!.tag = 3
        askLabel!.textAlignment = NSTextAlignment.right
        askLabel!.font = UIFont.systemFont(ofSize: 16.0)
        askLabel!.textColor = UIColor.black
        askLabel!.autoresizingMask = UIViewAutoresizing.flexibleHeight
        contentView.addSubview(askLabel!)
        
        accessoryType = .disclosureIndicator
    }
    
    required init?(coder aDecoder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }
}
