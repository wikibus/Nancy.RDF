﻿Feature: Serializing to JSON-LD
	Test serializing models to JSON-LD

@Brochure
@JsonLd
Scenario: Serialize simple model to JSON-LD
	Given A model with content:
	| Property | Vale |
	| Title    | Jelcz M11 - mały, stary autobus |
	And @context is: 
		"""
		'http://wikibus.org/contexts/brochure.jsonld'
		"""
	When model is serialized
	Then json object should contain key 'title' with value 'Jelcz M11 - mały, stary autobus'
	Then json object should contain key '@context' with value 'http://wikibus.org/contexts/brochure.jsonld'
	
@Brochure
@JsonLd
Scenario: Skip null properties when serializing model to JSON-LD
	Given A model with content:
	| Property    | Vale                            |
	| Title       | Jelcz M11 - mały, stary autobus |
	When model is serialized
	Then json object should not contain key 'description'