# PNGNormalizer

Provides PNG normalization for files that have been crushed, especially PNGs optimized for iOS with Apple's version of pngcrush.

## How To Use
This example shows how to read a crushed PNG, named *Icon@2x.png*, and save a normalized version as *Icon@2x.clean.png*.

```C#
    // Get crushed PNG file as a byte array
    byte[] sourcePngData = System.IO.File.ReadAllBytes("Icon@2x.png");
  
    // Parse the crushed data
    var png = new PngFile(sourcePngData);
    
    // Get the normalized PNG data
    byte[] normalizedPngData = png.Data;
    
    // Save the normalized file
    System.IO.File.WriteAllBytes("Icon@2x.clean.png", normalizedPngData);
```