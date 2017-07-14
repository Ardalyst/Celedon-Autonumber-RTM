# Celedon Partners Dynamics CRM AutoNumber
Provides auto-numbering to Dynamics CRM.

## Build
```{r, echo=FALSE, eval=TRUE, results="asis"}
travis_url <- "https://travis-ci.org/BESDev/Celedon-Autonumber-RTM.svg?branch="
shield <- paste0("[![Build Status](",
                 travis_url,
                 system("git rev-parse --abbrev-ref HEAD", intern = TRUE),
                 ")](https://travis-ci.org/BESDev/Celedon-Autonumber-RTM)")
cat(shield)
```
**Build status**

master: 
[![Build Status](https://travis-ci.org/Celedon-Autonumber-RTM/miniCRAN.svg?branch=master)](https://travis-ci.org/Celedon-Autonumber-RTM/miniCRAN)
release:
[![Build Status](https://travis-ci.org/Celedon-Autonumber-RTM/miniCRAN.svg?branch=release)](https://travis-ci.org/Celedon-Autonumber-RTM/miniCRAN)
dev: [![Build Status](https://travis-ci.org/Celedon-Autonumber-RTM/miniCRAN.svg?branch=dev)](https://travis-ci.org/Celedon-Autonumber-RTM/miniCRAN)

## How To Build
You required the following to build AutoNumber:

* [Microsoft Visual Studio 2015](https://www.visualstudio.com/vs/older-downloads/)
* [CRM Developer Toolkit - by Jason Lattimer](https://github.com/jlattimer/CRMDeveloperExtensions)

The current version builds against the Dynamics CRM 2013 SDK

## What it does right now, v1.1:
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
