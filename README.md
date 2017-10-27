# Celedon Partners Dynamics CRM AutoNumber
Provides auto-numbering to Dynamics CRM.

**Build status**

release:
[![Build Status](https://travis-ci.org/BESDev/Celedon-Autonumber-RTM.svg?branch=release)](https://travis-ci.org/BESDev/Celedon-Autonumber-RTM/branches)
master:
[![Build Status](https://travis-ci.org/BESDev/Celedon-Autonumber-RTM.svg?branch=master)](https://travis-ci.org/BESDev/Celedon-Autonumber-RTM/branches)
dev: [![Build Status](https://travis-ci.org/BESDev/Celedon-Autonumber-RTM.svg?branch=develop)](https://travis-ci.org/BESDev/Celedon-Autonumber-RTM/branches)

## How To Build
The following is required to build AutoNumber:

* [Microsoft Visual Studio 2015 or 2017](https://www.visualstudio.com/vs/older-downloads/)
* [CRM Developer Toolkit - by Jason Lattimer](https://github.com/jlattimer/CRMDeveloperExtensions)

> The current version builds against the Dynamics CRM 2016 - v6.0 SDK and .Net 4.0. You can [look here](https://blogs.msdn.microsoft.com/crm/2017/02/01/dynamics-365-sdk-backwards-compatibility/) for more information on SDK compatibilities. Since this solution does not connect to CRM Via alternative methods we do not need to update the connectivity support that changed in the later versions of CRM-Online for OAuth support.

## v1.2
> The plugin distributed with this version is *NOT* compatible with previous versions. You can import this Solution over the existing as an upgrade but you will need to convert the existing auto-number steps to the new plug-in in order to maintain support.

* Updated Code Formatting - *ReSharper* all the things!
* Refactored code to be *Thread Safe*
* Packaged Solution is exported for v8.0 (2016 RTM) - this means support for the v1.2 Solution is supported in 8.x + (Including 9.0) versions of CRM.
* SDK Version is set to 6.0.0
* Added back the test cases and converted to NUnit so that travis-ci can build

## v1.1:
* Supports custom prefix and suffix
* Configurable number of digits
* All parameters except the entityname and attributename can be modified at any time
* Supports multiple autonumbers on the same entity
* Generated numbers guaranteed to be unique, even in load balanced environments, including CRM Online
* Displays a live preview of the autonumber on the config form
* Validates entity and attributes are valid, and that there are no duplicate entries
* Supports conditional number generation (eg: allows different account types (or whatever) to get different number formats)
* Supports Activating/Deactivating AutoNumbers
* Allows runtime parameters to be entered into the Prefix and Suffix fields (See below for instructions)
* Runtime parameters now support looking up to parent record values
* Supports nested conditional parameters ie: "else if" conditions
* NEW: Supports 0 digit numbers (allows fully custom calculated fields, without adding any number)
* NEW: Added ability to generate random strings
* NEW: Can now trigger generation on either a Create OR Update event of a record

@See Documentation.md for usage.
