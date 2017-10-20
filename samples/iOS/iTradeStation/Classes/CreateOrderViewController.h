
#import "OffersController.h"

@interface CreateOrderViewController : UIViewController <UITextFieldDelegate> {
    IBOutlet UILabel *instrumentLabel;
    IBOutlet UISegmentedControl *sellBuyControl;
    IBOutlet UITextField *amountField;
    IBOutlet UISlider *amountSlider;
    IBOutlet UITextField *rateField;
    IBOutlet UISlider *rateSlider;
    IBOutlet UISegmentedControl *orderTypeControl;
    IBOutlet UIButton *cancelButton;
    IBOutlet UIButton *okButton;
    NSInteger offerIndex;
    COffersController *pO2g;

}

@property (nonatomic, retain) UILabel *instrumentLabel;
@property (nonatomic, retain) UISegmentedControl *sellBuyControl;
@property (nonatomic, retain) UITextField *amountField;
@property (nonatomic, retain) UISlider *amountSlider;
@property (nonatomic, retain) UITextField *rateField;
@property (nonatomic, retain) UISlider *rateSlider;
@property (nonatomic, retain) UISegmentedControl *orderTypeControl;
@property (nonatomic, retain) UIButton *okButton;
@property (nonatomic, retain) UIButton *cancelButton;
@property (nonatomic, assign) NSInteger offerIndex;

- (IBAction)cancelPressed;
- (IBAction)okPressed;
- (IBAction)sellBuySwitched;
- (IBAction)amountSliderChanged;
- (IBAction)rateSliderChanged;
- (IBAction)amountFieldChanged;
- (IBAction)rateFieldChanged;
- (void)refreshData;
- (id)initWithMyNibName:(NSString *)nibNameOrNil offerIndex:(NSInteger)index;

@end

