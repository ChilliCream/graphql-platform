It can be a little confusing installing and using all the tools to get Strawberry Shake working. So I cobbled together a Visual Studio Template to ease the process.

###### Installation

* Zip the files together and place them in \Documents\Visual Studio 2019\Templates\ProjectTemplates\Visual C#
* Upon restart the project template should now be available if you search for "Hot Chocolate Strawberry Shake Client"
* Copy your schema from the playground, or other source, into Schema.graphql
* Edit berry.json to set your Project Name, name of the generated class, and url
* Add your query/queries into queries.graphql
* Upon building the project a "generated" folder will appear with your classes 
          
###### Notes                                           
* You can also create new .graphql files and they will be detected. 
* Currently the generator doesn't look for .graphql files in folders, but this will be fixed before release
* Queries are encapsulated in a "query thisIsTheMethodName"-block, where what you would run in the playground is within the brackets. "thisIsTheMethodName" is the name of the Method that will be generated


This was put together in ten minutes so might still need some tweaking :) Enjoy!


 

