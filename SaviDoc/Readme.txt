Calling SaviDoc:

Example:

	SaviDoc.exe -s "X:\Data\WF\Server\sas" -d "z:\scratch\SaviDocOut" -e "X:\Data\WF\Server\sas\archive" -l "z:\scratch\savidoc.log" -u
	Use SaviDoc.exe --help to see options


SaviDoc Documentation:

Usage:

	SaviDoc supports the documentation of SAS code.
	SaviDoc uses pipes as delimiters within statements

Sample header:

/*---------------------------------------------------------------------------*
COMPANY    : ABC, LLC                                                  
LOCATION   : Colorado Springs, CO                                             
AUTHORS    : Chris A 
           : Alan C                                                  
NAME       : Dynamic Code Generator 
SUPPORT    : Chris A                                                    
SAS VERSION: SAS 9.4M5                                                        
DESCRIPTION: This code generates 2 levels of SAS code to support models        
           : The 2 levels are the conditional (ex. WHEN (VAR1 = '5') AND (VAR2 = 'MX')
           :    and the assignment: VAR3 = 0.56;
           : A third value is also created which is the OTHERWISE.
USAGE      :                                                                 
REMARKS    :  
EVENT      : ac | 31OCT2017 | Initial coding                                
EVENT      : ca | 13NOV2017 | Created because I wanted to experiment with some minor edits to Alan*s code
EVENT      : ca | 02NOV2018 | Added ability to use dates as keys
*---------------------------------------------------------------------------*/

Sample comment:


/*---------------------------------------------------------------------------*
MACRO      : CodeGenReferenceTable
IN         : refLib               | The SAS library containing the factor tables
IN         : refTable             | A Table in refLib with factors
OUT        : metaDataTable        | A dataset with metadata on refTable
DESCRIPTION: Runs the code generation steps. See detailed descriptions below.
*---------------------------------------------------------------------------*/

Notes:

- MACRO above can also be STEP, SECTION, MACRO, or FUNCTION
- Default values for MACROs can add a third parameter so that it is reflected. For example, in the above:
        IN         : refLib               | The SAS library containing the factor tables | MYLIB




