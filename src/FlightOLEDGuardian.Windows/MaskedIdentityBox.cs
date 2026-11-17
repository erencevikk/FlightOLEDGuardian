using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FlightOLEDGuardian.Windows;

// A masked field is a real PasswordBox: plaintext TextBoxes are removed, not merely covered.
public sealed class MaskedIdentityBox : UserControl
{
    public static readonly DependencyProperty ValueProperty=DependencyProperty.Register(nameof(Value),typeof(string),typeof(MaskedIdentityBox),new FrameworkPropertyMetadata("",FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,ValueChanged));
    public static readonly DependencyProperty IsMaskedProperty=DependencyProperty.Register(nameof(IsMasked),typeof(bool),typeof(MaskedIdentityBox),new PropertyMetadata(true,MaskChanged));
    private PasswordBox? password;
    private TextBox? text;
    private bool updating;
    public string Value { get=>(string)GetValue(ValueProperty);set=>SetValue(ValueProperty,value); }
    public bool IsMasked { get=>(bool)GetValue(IsMaskedProperty);set=>SetValue(IsMaskedProperty,value); }
    public MaskedIdentityBox(){Margin=new Thickness(0,4,12,10);Rebuild();}
    private static void ValueChanged(DependencyObject owner,DependencyPropertyChangedEventArgs args)=>((MaskedIdentityBox)owner).Sync();
    private static void MaskChanged(DependencyObject owner,DependencyPropertyChangedEventArgs args)=>((MaskedIdentityBox)owner).Rebuild();
    private void Rebuild()
    {
        updating=true;
        if(text!=null){text.Text="";text=null;}
        if(password!=null){password.Password="";password=null;}
        Content=null;
        if(IsMasked)
        {
            password=new PasswordBox{Password=Value??"",PasswordChar='●',Padding=new Thickness(10,8,10,8),Background=new SolidColorBrush(Color.FromRgb(23,35,50)),Foreground=new SolidColorBrush(Color.FromRgb(228,237,246)),BorderBrush=new SolidColorBrush(Color.FromRgb(48,70,92))};
            password.PasswordChanged+=(_,_)=>{if(!updating)SetCurrentValue(ValueProperty,password!.Password);};Content=password;
        }
        else
        {
            text=new TextBox{Text=Value??"",Margin=new Thickness(0)};
            text.TextChanged+=(_,_)=>{if(!updating)SetCurrentValue(ValueProperty,text!.Text);};Content=text;
        }
        updating=false;
    }
    private void Sync()
    {
        if(updating)return;updating=true;
        if(password!=null && password.Password!=Value)password.Password=Value??"";
        if(text!=null && text.Text!=Value)text.Text=Value??"";
        updating=false;
    }
}
