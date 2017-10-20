//{{NO_DEPENDENCIES}}


#define G_FX_VER_PRODUCT_VER_MAJOR  01
#define G_FX_VER_PRODUCT_VER_MINOR  02
#define G_FX_VER_PRODUCT_VER_DATE   051507

//
#define VER_SEP_COMMA ,
#define VER_SEP_DOT   .

#define VER_STR_HELPER_3(x,y,z,sep) #x #sep #y #sep #z
#define VER_STR_HELPER_4(x,y,z,b,sep) #x #sep #y #sep #z #sep #b 

#define VER_STR_HELPER3(x,y,z,sep) VER_STR_HELPER_3(x,y,z,sep)
#define VER_STR_HELPER4(x,y,z,b,sep) VER_STR_HELPER_4(x,y,z,b,sep)

//Comma separated version number.
#define VER_NUM_HELPER3(a,b,c) a,b,c
#define VER_NUM_HELPER4(a,b,c,d) a,b,c,d 


#define G_FX_VER_COMPNAME  "\0"
#define G_FX_VER_LEGALCOPYRIGHT "\0"


//Dot(.) separated version number.
#define G_FX_VER_PRODUCT_VERSION_STR VER_STR_HELPER3(G_FX_VER_PRODUCT_VER_MAJOR, \
                                                     G_FX_VER_PRODUCT_VER_MINOR, \
                                                     G_FX_VER_PRODUCT_VER_DATE,  \
                                                     VER_SEP_DOT)

//Comma(,) separated version number.
#define G_FX_VER_PRODUCT_VERSION_NUM VER_NUM_HELPER3(G_FX_VER_PRODUCT_VER_MAJOR, \
                                                     G_FX_VER_PRODUCT_VER_MINOR, \
                                                     G_FX_VER_PRODUCT_VER_DATE)  
                                        
                                                     
