# This README is for nexar supply VBA example

## Files Included

* `supplyExample.cls` is a class you can import into your excel sheet to use.

* `supplyExample.xlsm` is a excel sheet with the code included in the sheet rather than an imported class.

## This guide is for starting the excel file.

### Start-up 

* Open the `supplyExample.xlsm` file

* Click on the developer tab at the top
![](docs/developerRibbon.png?raw=true)

**If you do not have the developer ribbon, follow this [guide](https://support.microsoft.com/en-gb/office/show-the-developer-tab-e1192344-5e56-4d45-931b-e5fd9bea2d45#:~:text=On%20the%20File%20tab%2C%20go,select%20the%20Developer%20check%20box) to enable it**

* Then click `Visual Basic` and open `Sheet1(Sheet1)`. This is where the code for getting supply data lives.

* Copy and paste your Nexar API key into the `apikey.txt` file. You can get this from [here](https://portal.nexar.com/)

* Change the `filePath` variable to the location of this file.

* Close Visual Basic.

* On the excel sheet, on the developer tab, click `macros` and run `Sheet1.runSupplyExample`.