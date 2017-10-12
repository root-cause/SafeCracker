var Resolution = API.getScreenResolutionMaintainRatio();
var safeNearby = false;
var safeCracking = false;
var renderAngle = 0.0;
var actualAngle = null;
var angleDiff = [4.5, 3.5, 2.5, 1.75, 1.0];
var renderAngleDiff = 1.0;

function numDiff(num1, num2) {
    return (num1 > num2) ? num1-num2 : num2-num1
}

function generateRandomNumber(difference) {
    var min = angleDiff[difference] / 2,
        max = angleDiff[difference],
        highlightedNumber = (Math.random() * (max - min) + min);

    return highlightedNumber;
};

function moveDial(left) {
    API.playSoundFrontEnd("Faster_Click", "RESPAWN_ONLINE_SOUNDSET");

    if (left) {
        renderAngle += renderAngleDiff;
        if(renderAngle >= 360.0) renderAngle = 0.0;
    } else {
        renderAngle -= renderAngleDiff;
        if(renderAngle <= 0.0) renderAngle = 360.0;
    }
}

API.onServerEventTrigger.connect(function(event_name, args) {
    switch (event_name)
    {
        case "SetSafeNearby":
            safeNearby = args[0];

            if (safeNearby) API.playSoundFrontEnd("WAYPOINT_SET", "HUD_FRONTEND_DEFAULT_SOUNDSET");
        break;

        case "SetDialInfo":
            renderAngle = 0.0;
            actualAngle = args[0];
            safeCracking = args[1];
        break;
    }
});

API.onKeyDown.connect(function(sender, e) {
    if (e.KeyCode == Keys.E)
    {
        if (API.isChatOpen() || !safeNearby) return;

        if (!safeCracking) {
            API.triggerServerEvent("InteractSafe");
        } else {
            API.triggerServerEvent("OpenSafe", renderAngle);
        }
    }

    if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
    {
        if (safeCracking) moveDial((e.KeyCode == Keys.Left));
    }
});

API.onUpdate.connect(function(sender, args) {
    if (safeNearby) API.displaySubtitle("Press ~y~E ~w~to interact with the safe.", 100);

    if (!safeCracking) return;
    var diff = numDiff(renderAngle, actualAngle);
    var extra = (diff < 5) ? generateRandomNumber(diff) : 0.0;
    API.drawGameTexture("MPSafeCracking", "Dial_BG", ((Resolution.Width / 2) - 250) + extra, ((Resolution.Height / 2) - 250) + extra, 512, 512, 0, 255, 255, 255, 255);
    API.drawGameTexture("MPSafeCracking", "Dial", ((Resolution.Width / 2) - 123) + extra, ((Resolution.Height / 2) - 123) + extra, 256, 256, renderAngle, 255, 255, 255, 255);
});