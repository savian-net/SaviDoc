/*---------------------------------------------------------------------------*
COMPANY    : Westfield Group                                                  
LOCATION   : Westfield Center, OH                                             
AUTHORS    : Chris Allard 
           : Alan Churchill                                                  
NAME       : Dynamic Code Generator 
SUPPORT    : Chris Allard                                                    
SAS VERSION: SAS 9.4M4                                                        
DESCRIPTION: This code generates 2 levels of SAS code to support pricing guidance        
           : The 2 levels are the conditional (ex. WHEN (VAR1 = '5') AND (VAR2 = 'IL')
           :    and the assignment: VAR3 = 0.56;
           : A third value is also created which is the OTHERWISE.
USAGE      :                                                                 
REMARKS    :  
EVENT      : alchur  | 31OCT2017 | Initial coding                                
EVENT      : callard | 13NOV2017 | Created because I wanted to experiment with some minor edits to Alan*s code
EVENT      : callard | 13NOV2017 | Added reflib as an input so I can test using a library not called SQLSRVR (default value for backwards compatibility)
EVENT      : callard | 13NOV2017 | Modified code to allow for output of multiple value fields
EVENT      : alchur  | 17JAN2018 | Added support for wildcards (callard did algorithm), default values, and variable to variable assignment.
EVENT      : callard | 05FEB2018 | Modified to support Bobby*s format tables (e.g. allow min=max in range lookups)
EVENT      : callard | 20FEB2018 | Big changes
EVENT      : callard | 23MAR2018 | Modified output logic. Writing nulls still moves the cursor by 1 so we need an extra -1 for this condition.
EVENT      : callard | 25MAR2018 | Changed join condition output field from CONDITION to codeGenMatchCondition
EVENT      : callard | 09MAY2018 | Updated field names to match database (type->matchType, table->DbTable, remove assignmentVar)
EVENT      : callard | 25SEP2018 | Updated field names again (column name is now stored in sourceVariable not rangeStart, range columns are only used for range matches, DbTable->RefTable since tables will not necessarily be on a database)
EVENT      : callard | 02NOV2018 | Added ability to use dates as keys
*---------------------------------------------------------------------------*/


/*---------------------------------------------------------------------------*
MACRO      : CodeGenReferenceTable
IN         : refLib               | The SAS library containing the factor tables
IN         : refTable             | A Table in refLib with factors
IN         : metaDataTable        | A Table in refLib with metadata on refTable
IN         : CgConditionFilename  | pointer to SAS filename where intermediate where condition will be placed
IN         : CgFinalFilename      | pointer to SAS filename where FINAL code will be placed
IN         : debugYN              | When Y, saves intermediate work tables and writes macro vars to the log
DESCRIPTION: Runs the code generation steps. See detailed descriptions below.
*---------------------------------------------------------------------------*/

%macro CodeGenReferenceTable(
    reflib=,
    refTable=,
    metaDataTable=,
    CgConditionFilename=,
    CgFinalFilename=,
    debugYN=N
);

/*---------------------------------------------------------------------------*
SECTION    : Test to see if the dataset exists
DESCRIPTION: Checks for obs in the dataset
*---------------------------------------------------------------------------*/
    %if not %sysfunc(exist(&reflib..&refTable.)) %then %goto macroexit;
	
/*---------------------------------------------------------------------------*
SECTION    : Get data types for variables
DESCRIPTION: Gets the data types of the variables (1=numeric, 2=string)
*---------------------------------------------------------------------------*/
    proc contents data=&reflib..&refTable. noprint out=refTableVarTypes (keep=Name Type Format rename=(type=varType)); run;


/*---------------------------------------------------------------------------*
SECTION    : Get metadata for reference table
DESCRIPTION: Gets metadata from &MetaDataTable for a given &RefTable
*---------------------------------------------------------------------------*/
    data metadataHelper0;
    set &reflib..&metaDataTable. (keep=RefTable sourceVariable rangeStart rangeEnd MatchType wildcard);
        where RefTable = "&refTable.";
        rowNumber=_n_;
    run;


/*---------------------------------------------------------------------------*
SECTION    : Append variable type (1=numeric, 2=string) to metadata
DESCRIPTION: Merges the variable type data into the metadata
           :   For RANGE variables, use rangeStart to match
           :   For Non-Range vars (EXACT Match and OUTPUT), use sourceVariable
*---------------------------------------------------------------------------*/
    proc sql;
        create table metadataHelper as
        select a.*, b.varType, b.Format,
            case when b.Format in 
                (
                    'DATE','DAY','DOWNAME','WEEKDATE','WORDDATE','WORDDATX',
                    'DDMMYY','DDMMYYB','DDMMYYC','DDMMYYD','DDMMYYN','DDMMYYP','DDMMYYS',
                    'MMDDYY','MMDDYYB','MMDDYYC','MMDDYYD','MMDDYYN','MMDDYYP','MMDDYYS'               
                ) 
            then 'Y' 
                else 'N' 
                end as IsDateYN
        from metadataHelper0 as a left outer join refTableVarTypes as b
            on  (upcase(a.sourceVariable) = upcase(b.name) AND upcase(a.matchType) not in ('RANGELOW','RANGEHIGH'))
            OR  (upcase(a.rangeStart) = upcase(b.name) AND upcase(a.matchType) in ('RANGELOW','RANGEHIGH'))
        order by a.rowNumber
        ;
    quit;


/*---------------------------------------------------------------------------*
SECTION    : Error Handling: Ref table incomplete
DESCRIPTION: Stops processing when metadata table defines columns missing from &RefTable
*---------------------------------------------------------------------------*/
    ** Missing Var Count **;    
    proc sql noprint;
    select count(*) into: RefTableVarTypeMissingCount
    from metadataHelper
    where varType is null
    ;
    quit;
    %put ....;
    %put RefTableVarTypeMissingCount=&RefTableVarTypeMissingCount;
    %put ....;
    
    ** If count NE 0, stop processing **;
    %if %eval(&RefTableVarTypeMissingCount.) NE 0 %then %do;
        %put FAILURE: Did not find metadata for all columns in &reflib..&RefTable.;
        %put ....;
        %put ....Stopping macro processing;
        %put ....;
        %goto macroExit;
    %end;

/*---------------------------------------------------------------------------*
SECTION    : Error Handling: Metadata table incomplete
DESCRIPTION: Stops processing when &RefTable columns are missing in metadata
           :  - Not currently in effect!
           :  - Currently, &RefTable is expected to have superfluous columns
           :      (such as elementVersion and ID)
           :  - Not sure how to handle, but in the long run this should cause an error to occur!
           :      - Could maybe whitelist elementVersion and ID but that seems overly restrictive
           :      - Could require 1:1 mapping between RefTable and Metadata and 
           :        assume service will kill these superfluous rows?
*---------------------------------------------------------------------------*/


/*---------------------------------------------------------------------------*
SECTION    : Create PRELIMINARY codegen SAS code with WHEN condition
DESCRIPTION: First pass is based on METADATA only!
           :   1. Generates WHEN condition for non-Output variables
           :   2. Stores output variable information in macro variables
           : Possible MathcType values are:
           :    RANGELOW  - A range that favors the low end. The low is a GE vs GT
           :    RANGEHIGH - A range that favors the high end. The high is a GE vs GT
           :    EXACT     - An exact match on a value
           :    OUTPUT    - A value that is emitted as output if the conditions are met.
*---------------------------------------------------------------------------*/
    data _null_; *data conditionstest;
        attrib conditions length=$2000 
            line       length=$500
        ;
        set metadataHelper end=eof;
        retain conditions;



        ** SELECT WHEN piece **;
        select (upcase(MatchType));

        ** Create various conditions for Match fields **;
        when ('RANGELOW')
            do ;
                line = "PUT @13 '( (' " || trim(rangeStart) || "'<= " || trim(sourceVariable) || " < ' " || trim(rangeEnd) || " ') OR (' "|| trim(rangeStart) || "'= " || trim(sourceVariable) || " = ' " || trim(rangeEnd) ||" ') )' ;";    
            end; *End RangeLow condition;


        when ('RANGEHIGH')
            do ;
                line = "PUT @13 '( (' " || trim(rangeStart) || "'< " || trim(sourceVariable) || " <= ' " || trim(rangeEnd) || " ') OR (' "|| trim(rangeStart) || "'= " || trim(sourceVariable) || " = ' " || trim(rangeEnd) ||" ') )' ;";    
            end; *End RangeHigh condition;


        when ('EXACT')
            do ;

            if (vartype = 1) then 
                do;
                    if wildcard eq '' then line = "PUT @13 '(' " || " ' " || trim(sourceVariable) || " = ' " || strip(sourceVariable) || " ')' ;";                  
                    else if wildcard ne '' then line = "PUT @13 '( (' " || " '" || trim(sourceVariable) || " = ' " || strip(sourceVariable) || " ')" || " OR " || "( ' " ||trim(sourceVariable) || " ' = " || trim(wildcard) || ") )' ;";                  
                end;

            if (vartype = 2) then
                do;
                    if wildcard eq '' then line = "PUT @13 '(UPCASE(" || trim(sourceVariable) || ") = UPCASE(""' " || trim(sourceVariable) || "+(-1) '"|| '"' || "))' ;"; 
                    else if wildcard ne '' then line = "PUT @13 '( (UPCASE(" || trim(sourceVariable) || ") = UPCASE(""' " || trim(sourceVariable) || "+(-1) '"|| '"' || ")) OR (UPCASE(""' " || trim(sourceVariable) || "+(-1) '" || '"' || ") = " || '"' || trim(wildcard) || '"' || ") ) ' ;"; 
                end;
            end; * End Exact condition;


        ** OUTPUT fields are not written to the intermediate file. Rather, their values are stored in macro variables;
        when ('OUTPUT')
            do ;
                ii+1;
                x = strip(ii);
                call symputx('outVar'||x, trim(sourceVariable), 'L'); *Value1, Value2, ...;
                call symputx('outVarType'||x,vartype, 'L'); *1/2 = num/char;
                call symputx('numValues',x, 'L'); * numValues is the total number of outputs;
                /*call symputx('assignVar'||x, strip(assignmentVar), 'L'); * assignVar is the value of the assignmentVar;*/
            end; *End Output condition;


        end; *Close Select Statement;
 

        ** Clean-Up **;
        if upcase(MatchType) NE 'OUTPUT' and line ne '' then do;

            *For first line, set up conditions;
            if strip(conditions)="" then conditions = trim(line);

                *For later lines, concatenate & add a separate line with AND statement;
                else conditions = trim(conditions) || '0A'x || "PUT @17 'AND';"  || '0A'x || trim(line); 
        end;
       

        ** Output on last line only **;
        if eof then
            do ;
                file &CgConditionFilename. ;
                if strip(conditions)="" then conditions=" PUT @13 '(1 = 1)' ;";
                put conditions; 
            end;
    run; 


/*---------------------------------------------------------------------------*
SECTION    : Reformat Date Variables
DESCRIPTION: This step defines and runs a macro to scrub dates from the factor tables:
           :   1. Identifies date-formatted fields with proc contents
           :   2. Uses attribs to reassign to 8. format&length 
*---------------------------------------------------------------------------*/
%macro RemoveDateFormats(infile=,outfile=,debugYN=N);
proc contents data=&infile. noprint out=rdfInfileContents (keep=Name Type Format rename=(type=varType)); run;

data rdfInfileContents2;
set rdfInfileContents;

%let NumDateVars=0;
if Format in          
    ('DATE','DAY','DOWNAME','WEEKDATE','WORDDATE','WORDDATX',
    'DDMMYY','DDMMYYB','DDMMYYC','DDMMYYD','DDMMYYN','DDMMYYP','DDMMYYS',
    'MMDDYY','MMDDYYB','MMDDYYC','MMDDYYD','MMDDYYN','MMDDYYP','MMDDYYS'               
    ) 
then do; %*date format loop;
    numDateVars+1;
    call symputx('DateVar'||strip(numDateVars),Name);
    call symputx('numDateVars',numDateVars);

end; %* end date format loop;
run;


%** Write date macro vars to log to assist debugging;
%if &numDateVars>0 %then %do;
    %put NumDateVars:   &numDateVars.;
    %put 1st Date Var:  &DateVar1.;
    %put Last Date Var: &&DateVar&numDateVars..;
%end; 


%** Create outfile with attribs removing date formats;
data &outfile.;
%if &numDateVars>0 %then %do; %* date fix loop;
    attrib
    %do dd = 1 %to &numDateVars.; %* var loop;
        &&DateVar&dd. format=8. length=8.
    %end; %* end var loop;
    ; %* <= This semicolon ends the attrib;

%end; %* end date fix loop;

set &infile.;
run;


%if &debugYN=N %then %do;
    proc delete data=rdfInfileContents; run;
    proc delete data=rdfInfileContents2; run;
%END;
%mend;

%RemoveDateFormats(infile=&reflib..&refTable.,outfile=&refTable.,debugYN=&debugYN.);


/*---------------------------------------------------------------------------*
SECTION    : Create FINAL codegen SAS code
DESCRIPTION: This step creates the final SAS code based on:
           :   1. The data in &Reftable
           :   2. Information created/stored in the prior step:
           :     2a. The WHEN condition in the PRELIMINARY codegen SAS code
           :     2b. The OUTPUT information stored in macro variables
*---------------------------------------------------------------------------*/
    data _null_; *data TEST ;
        file &CgFinalFilename. ;
        set &refTable. end=eof; %* work file is the new date-scrubbed copy;

        * Build out Select When Statement from preliminary codegen file and Reference data;
        if _n_ = 1 then
            put @1 'SELECT; ';

        put   /@5 'WHEN (';

        %include &CgConditionFilename. ; *Conditions logical condition;

        put   @10 ') '; *Ends When statement;

        * Create output statement;
        put
              @9  'DO;'
            / @13 'codeGenMatchCondition = ' _N_ ';'
        ; *Closes put statement;
           
            %do i = 1 %to &numValues.;
                %if &&outVartype&i.. = 1 %then 
                %do;
                    put @13 "&&outVar&i.. = " &&outVar&i.. ';'; %* left side is variable name, right side is value;
                %end;

                %else %if &&outVartype&i.. = 2 %then 
                %do;
                    %* left side is variable name, right side is value;
                    if &&outVar&i..='' then put @13 "&&outVar&i.. = '" &&outVar&i.. +(-2) "';";
                    else put @13 "&&outVar&i.. = '" &&outVar&i.. +(-1) "';";
                %end;

            %end;  %* End numValues Loop;     

        put
           / @9 'END;'
            ; *Ends Output put statement;


        * On last row, create otherwise statement, setting output values to null;
        if eof then
        do ;
            put 
                /@5  'OTHERWISE'
                /@9  'DO; '   
                /@13 'codeGenMatchCondition=-99999; '
                  

            %do i = 1 %to &numValues.;
            
            %if &&outVartype&i.. = 1 %then %do;
                    / @13 "&&outVar&i.. = .;"
                %end;
                %else %if &&outVartype&i.. = 2 %then %do;
                    / @13 "&&outVar&i.. = '';" 
                %end;                
            %end; %* end numValues Loop;
            ;

            put  
                 @9 'END; '   
                /@1 'END;' 
            ;
        end; *End eof Do;

    run; 


/*---------------------------------------------------------------------------*
SECTION    : Debug step
DESCRIPTION: If &DEBUGYN is set to Y, additional information is saved in the log
           :   and the work datasets are not deleted.
*----------------------------------------------------------------------------*/
    %if &debugYN=N %then 
        %do;
           proc delete data=metadataHelper0 metadataHelper refTableVarTypes; run;
        %end;
    %else
        %do;
            %put Total Number of Output Variables = &numValues.;
            %do i = 1 %to &numValues. ;
                %put VarNum = &i.;
                %put ..OutVar = &&outVar&i..;
                %put ..OutVarType = &&OutVarType&i..;
                %put;
            %end;
        %end;
%macroexit:
%mend CodeGenReferenceTable;

%macro multigen(table=);
	filename cgcondtn "&outpath.\cg_&table..sas";
	filename cgselect "&outpath.\&table..sas";

	%CodeGenReferenceTable(
		reflib=cgsas,
		refTable=&table.,
		metaDataTable=RefPgCodegenMetadata,
		CgConditionFilename=cgcondtn,
		CgFinalFilename=cgselect,
		debugYN=N
	);
%mend multigen;

