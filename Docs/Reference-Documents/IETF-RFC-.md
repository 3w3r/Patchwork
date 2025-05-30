---
Internet-Engineering-Task-Force: P. Bryan, Ed.
Internet-Draft: Salesforce.com
Intended-Status: Informational K. Zyp
Created: September 16, 2012
Expires: March 20, 2013
---

# JSON Reference - draft-pbryan-zyp-json-ref-03

This document is an Internet-Draft (I-D). Anyone may submit an I-D to the IETF. This I-D is not endorsed by the IETF and has no formal standing in the IETF standards process.

## Abstract

JSON Reference allows a JSON value to reference another value in a JSON document.

## Status of this Memo

This Internet-Draft is submitted in full conformance with the provisions of BCP 78 and BCP 79.

Internet-Drafts are working documents of the Internet Engineering Task Force (IETF). Note that other groups may also distribute working documents as Internet-Drafts. The list of current Internet- Drafts is at [IETF Drafts](http://datatracker.ietf.org/drafts/current/).

Internet-Drafts are draft documents valid for a maximum of six months and may be updated, replaced, or obsoleted by other documents at any time. It is inappropriate to use Internet-Drafts as reference material or to cite them other than as "work in progress."

This Internet-Draft will expire on March 20, 2013.

## Copyright Notice

Copyright (c) 2012 IETF Trust and the persons identified as the document authors. All rights reserved.

This document is subject to [BCP 78](https://datatracker.ietf.org/doc/html/bcp78) and the IETF Trust's Legal Provisions Relating to [IETF Documents](http://trustee.ietf.org/license-info) in effect on the date of publication of this document. Please review these documents carefully, as they describe your rights and restrictions with respect to this document. Code Components extracted from this document must include Simplified BSD License text as described in Section 4.e of the Trust Legal Provisions and are provided without warranty as described in the Simplified BSD License.

## Table of Contents

1. Introduction
2. Conventions
3. Syntax
4. Resolution
5. Error Handling
6. IANA Considerations
7. Security Considerations
8. Normative References
9. Appendix
   - Appendix A. Acknowledgements
   - Appendix B. Examples
   - Authors' Addresses

## 1. Introduction

This specification defines a JSON [RFC4627] structure which allows a JSON value to reference another value in a JSON document. This provides the basis for transclusion in JSON: the use of a target resource as an effective substitute for the reference.

## 2. Conventions

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC2119].

## 3. Syntax

A JSON Reference is a JSON object, which contains a member named `"\$ref"`, which has a JSON string value. Example:

```json
{ "$ref": "http://example.com/example.json#/foo/bar" }
```

If a JSON value does not have these characteristics, then it SHOULD NOT be interpreted as a JSON Reference.

The `"$ref"` string value contains a URI [RFC3986], which identifies the location of the JSON value being referenced. It is an error condition if the string value does not conform to URI syntax rules. Any members other than "\$ref" in a JSON Reference object SHALL be ignored.

## 4. Resolution

Resolution of a JSON Reference object SHOULD yield the referenced JSON value. Implementations MAY choose to replace the reference with the referenced value.

If the URI contained in the JSON Reference value is a relative URI, then the base URI resolution MUST be calculated according to [RFC3986], section 5.2. Resolution is performed relative to the referring document.

If a URI contains a fragment identifier, then the fragment should be resolved per the fragment resolution mechansim of the referrant document. If the representation of the referrant document is JSON, then the fragment identifier SHOULD be interpreted as a [JSON-Pointer].

## 5. Error Handling

In the event of an error condition, evaluation of the JSON Reference SHOULD fail to complete.

## 6. IANA Considerations

This draft includes no request to IANA.

## 7. Security Considerations

A JSON Reference is not guaranteed to resolve to a JSON value. Implementations of this specification SHOULD take appropriate precautions.

Documents containing JSON References can be structured to resolve cyclically. Implementations SHOULD include appropriate checks to prevent such structures from resulting in infinite recursion or iteration.

## 8. Normative References

- [JSON-Pointer](https://datatracker.ietf.org/doc/html/draft-ietf-appsawg-json-pointer-04) Bryan, P., Zyp, K., and M. Nottingham, "JSON Pointer", draft-ietf-appsawg-json-pointer-04 (work in progress), September 2012.

- [RFC2119] Bradner, S., "Key words for use in RFCs to Indicate Requirement Levels", BCP 14, RFC 2119, March 1997.

- [RFC3986] Berners-Lee, T., Fielding, R., and L. Masinter, "Uniform Resource Identifier (URI): Generic Syntax", STD 66, RFC 3986, January 2005.

- [RFC4627] Crockford, D., "The application/json Media Type for JavaScript Object Notation (JSON)", RFC 4627, July 2006.

## 9. Appendix

### Appendix A. Acknowledgements

The following individuals contributed ideas, feedback and wording to this specification:

> Bob Aman, Francis Galiegue.

### Appendix B. Examples

TBD.

Authors' Addresses

> Paul C. Bryan (editor)
> Salesforce.com
>
> Phone: +1 604 783 1481
> Email: pbryan@anode.ca
>
> Kris Zyp
> SitePen (USA)
>
> Phone: +1 650 968 8787
> Email: kris@sitepen.com
