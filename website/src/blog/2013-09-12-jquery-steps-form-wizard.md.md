---
path: "/blog/2013/09/12/jquery-steps-form-wizard"
date: "2013-09-12"
title: "How to create a Form Wizard using jQuery Steps"
author: "Rafael Staib"
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

This blog article was previously published on http://www.rafaelstaib.com/post/How-to-create-a-Form-Wizard-using-jQuery-Steps.

## Motivation

Sometimes it's better to separate a large or complex form into different sections. It’s because your form looks much cleaner and less difficult. Despite that fact people want to be guided through complex processes without understanding those deeply.

## Situation

There are many options to realize such a form wizard. You could use for example just static HTML files for each step one and link them together. But this, actually, could be really frustrating for you and the people visiting your site. Think of maintaining an existing wizard (e.g. adding a new step or changing links) then you have to touch in worst case all the existing steps that are involved. On the other hand your visitors get frustrated because of the many page requests and their accompanying latency time. However, all this isn’t probably new for you. Therefore, let’s step over!

## Solution

Let me explain you how I usually solve this problem. I prefer using **jQuery Steps** a jQuery UI plugin because of its simplicity and feature-richness. And most important it’s free (open source). Just grab and use it!

But for now enough words - let’s get our hands dirty!

First of all, we will download **jQuery Steps** from [here](http://www.jquery-steps.com) and take the basic example markup from [there](http://www.jquery-steps.com/GettingStarted#basic) – done. Not really but it isn’t far away from being done.

```html
<!DOCTYPE html>
<html>
  <head>
    <title>Demo</title>
    <meta charset="utf-8" />
    <script src="jquery.js"></script>
    <script src="jquery.steps.js"></script>
    <link href="jquery.steps.css" rel="stylesheet" />
  </head>
  <body>
    <script>
      $("#wizard").steps();
    </script>
    <div id="wizard"></div>
  </body>
</html>
```

What else? We have to replace this `<div id="wizard"></div>` part by our own form markup and override the bodyTag property on initialization.

```html
<form id="form-3" action="#">
  <h1>Account</h1>
  <fieldset>
    <legend>Account Information</legend>

    <label for="userName">User name *</label>
    <input id="userName" name="userName" type="text" class="required" />
    <label for="password">Password *</label>
    <input id="password" name="password" type="text" class="required" />
    <label for="confirm">Confirm Password *</label>
    <input id="confirm" name="confirm" type="text" class="required" />
    <p>(*) Mandatory</p>
  </fieldset>

  <h1>Profile</h1>
  <fieldset>
    <legend>Profile Information</legend>

    <label for="name">First name *</label>
    <input id="name" name="name" type="text" class="required" />
    <label for="surname">Last name *</label>
    <input id="surname" name="surname" type="text" class="required" />
    <label for="email">Email *</label>
    <input id="email" name="email" type="text" class="required email" />
    <label for="address">Address</label>
    <input id="address" name="address" type="text" />
    <label for="age"
      >Age (The warning step will show up if age is less than 18) *</label
    >
    <input id="age" name="age" type="text" class="required number" />
    <p>(*) Mandatory</p>
  </fieldset>

  <h1>Warning</h1>
  <fieldset>
    <legend>You are to young</legend>

    <p>Please go away ;-)</p>
  </fieldset>

  <h1>Finish</h1>
  <fieldset>
    <legend>Terms and Conditions</legend>

    <input
      id="acceptTerms"
      name="acceptTerms"
      type="checkbox"
      class="required"
    />
    <label for="acceptTerms">I agree with the Terms and Conditions.</label>
  </fieldset>
</form>
```

This is just a normal form which you should be familiar with. The small difference [here](http://www.jquery-steps.com/Examples#advanced-form) is that we use a h1 tag on top of each fieldset tag. **jQuery Steps** needs that to build the wizard navigation. I grabbed that from here and there you can also see how it works in action.

The following code shows how to override the bodyTag property in order to tell **jQuery Steps** to use the fieldset tag as body container instead of div.

```javascript
$("#wizard").steps({
  bodyTag: "fieldset",
});
```

Actually, we are done but to offer users a rich and intuitive experience we will add an additional jQuery plugin which all of you very probably already know; **jQuery Validation** (for more Information see [here](http://jqueryvalidation.org/)). It's a plugin for doing form input validation. Furthermore, we will attach four event handler functions containing some extra magic. Finally, we will initialize **jQuery Validation**. Since both plugins are built on top of **jQuery**, we can make use of chaining (e.g. `$("#form").steps().validate()`). Okay, before we start adding more code take a brief look on the following table that explains the four events we will shortly add.

| Event            | Description                                                                                                                                             |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `onStepChanging` | Fires before the step changes and can be used to prevent step changing by returning `false`. Very useful for form validation or checking preconditions. |
| `onStepChanged`  | Fires after the step has change.                                                                                                                        |
| `onFinishing`    | Fires before finishing and can be used to prevent completion by returning `false`. Very useful for form validation or checking preconditions.           |
| `onFinished`     | Fires after completion.                                                                                                                                 |

These useful events will help us realizing pretty neat functionality. So the events ending on -ing will be invoked right after an user interaction but before any internal logic gets executed. Those events will be very helpful to prevent step changing and submission. The events ending with -ed will happen after everything is executed and let us execute custom logic (e.g. skipping a step and submitting a form via AJAX).

Internally, it's implemented like this:

```javascript
if (wizard.triggerHandler("stepChanging", [state.currentIndex, index])) {
  // Internal logic

  wizard.triggerHandler("stepChanged", [index, oldIndex]);
}
```

With that in mind you know how it works. The first event function we are going to add to the settings is `onStepChanging`. This Implementation allows us to react before things are going to change.

```javascript
onStepChanging: function (event, currentIndex, newIndex)
{
    // Always allow going backward even if the current step contains invalid fields!
    if (currentIndex > newIndex)
    {
        return true;
    }

    // Forbid suppressing "Warning" step if the user is to young
    if (newIndex === 3 && Number($("#age").val()) < 18)
    {
        return false;
    }

    var form = $(this);

    // Clean up if user went backward before
    if (currentIndex < newIndex)
    {
        // To remove error styles
        $(".body:eq(" + newIndex + ") label.error", form).remove();
        $(".body:eq(" + newIndex + ") .error", form).removeClass("error");
    }

    // Disable validation on fields that are disabled or hidden.
    form.validate().settings.ignore = ":disabled,:hidden";

    // Start validation; Prevent going forward if false
    return form.valid();
}
```

The second event function contains some logic to skip the warning step we added before.

```javascript
onStepChanged: function (event, currentIndex, priorIndex)
{
    // Suppress (skip) "Warning" step if the user is old enough and wants to the previous step.
    if (currentIndex === 2 && priorIndex === 3)
    {
        $(this).steps("previous");
        return;
    }

    // Suppress (skip) "Warning" step if the user is old enough.
    if (currentIndex === 2 && Number($("#age").val()) >= 18)
    {
        $(this).steps("next");
    }
}
```

The next two event functions allow us to handle submission and submission prevention.

```javascript
onFinishing: function (event, currentIndex)
{
    var form = $(this);

    // Disable validation on fields that are disabled.
    // At this point it's recommended to do an overall check (mean ignoring only disabled fields)
    form.validate().settings.ignore = ":disabled";

    // Start validation; Prevent form submission if false
    return form.valid();
}
```

The latter event function is required for form submission.

```javascript
onFinished: function (event, currentIndex)
{
    var form = $(this);

    // Submit form input
    form.submit();
}
```

The final JavaScript code looks like this after we stick everything together.

```javascript
$("#form")
  .steps({
    bodyTag: "fieldset",
    onStepChanging: function(event, currentIndex, newIndex) {
      // Always allow going backward even if the current step contains invalid fields!
      if (currentIndex > newIndex) {
        return true;
      }

      // Forbid suppressing "Warning" step if the user is to young
      if (newIndex === 3 && Number($("#age").val()) < 18) {
        return false;
      }

      var form = $(this);

      // Clean up if user went backward before
      if (currentIndex < newIndex) {
        // To remove error styles
        $(".body:eq(" + newIndex + ") label.error", form).remove();
        $(".body:eq(" + newIndex + ") .error", form).removeClass("error");
      }

      // Disable validation on fields that are disabled or hidden.
      form.validate().settings.ignore = ":disabled,:hidden";

      // Start validation; Prevent going forward if false
      return form.valid();
    },
    onStepChanged: function(event, currentIndex, priorIndex) {
      // Suppress (skip) "Warning" step if the user is old enough and wants to the previous step.
      if (currentIndex === 2 && priorIndex === 3) {
        $(this).steps("previous");
        return;
      }

      // Suppress (skip) "Warning" step if the user is old enough.
      if (currentIndex === 2 && Number($("#age").val()) >= 18) {
        $(this).steps("next");
      }
    },
    onFinishing: function(event, currentIndex) {
      var form = $(this);

      // Disable validation on fields that are disabled.
      // At this point it's recommended to do an overall check (mean ignoring only disabled fields)
      form.validate().settings.ignore = ":disabled";

      // Start validation; Prevent form submission if false
      return form.valid();
    },
    onFinished: function(event, currentIndex) {
      var form = $(this);

      // Submit form input
      form.submit();
    },
  })
  .validate({
    errorPlacement: function(error, element) {
      element.before(error);
    },
    rules: {
      confirm: {
        equalTo: "#password",
      },
    },
  });
```

Any questions or comments are very welcome!
