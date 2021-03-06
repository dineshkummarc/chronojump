#-*- R -*-

## Script from Fourth Edition of `Modern Applied Statistics with S'

# Chapter 10   Random and Mixed Effects

library(MASS)
library(lattice)
trellis.device(postscript, file="ch10.ps", width=8, height=6, pointsize=9)
options(echo=T, width=65, digits=5)
library(nlme)

# 10.1  Linear models

xyplot(Y ~ EP | No, data = petrol,
   xlab = "ASTM end point (deg. F)",
   ylab = "Yield as a percent of crude",
   panel = function(x, y) {
      panel.grid()
      m <- sort.list(x)
      panel.xyplot(x[m], y[m], type = "b", cex = 0.5)
   })

Petrol <- petrol
names(Petrol)
Petrol[, 2:5] <- scale(Petrol[, 2:5], scale = F)
pet1.lm <- lm(Y ~ No/EP - 1, Petrol)
matrix(round(coef(pet1.lm), 2), 2, 10, byrow = T,
       dimnames = list(c("b0", "b1"), levels(Petrol$No)))

pet2.lm <- lm(Y ~ No - 1 + EP, Petrol)
anova(pet2.lm, pet1.lm)

pet3.lm <- lm(Y ~ SG + VP + V10 + EP, Petrol)
anova(pet3.lm, pet2.lm)

pet3.lme <- lme(Y ~ SG + VP + V10 + EP,
                random = ~ 1 | No, data = Petrol)
summary(pet3.lme)

pet3.lme <- update(pet3.lme, method = "ML")
summary(pet3.lme)

anova(pet3.lme, pet3.lm)

pet4.lme <- update(pet3.lme, fixed = Y ~ V10 + EP)
anova(pet4.lme, pet3.lme)
fixed.effects(pet4.lme)
coef(pet4.lme)

pet5.lme <- update(pet4.lme, random = ~ 1 + EP | No)
anova(pet4.lme, pet5.lme)

nl1 <- nlschools
attach(nl1)
classMeans <- tapply(IQ, class, mean)
nl1$IQave <- classMeans[as.character(class)]
detach()
cen <- c("IQ", "IQave", "SES")
nl1[cen] <- scale(nl1[cen], center = T, scale = F)

options(contrasts = c("contr.treatment", "contr.poly"))
nl.lme <- lme(lang ~ IQ*COMB + IQave + SES,
              random = ~ IQ | class, data = nl1)
summary(nl.lme)

summary(lm(lang ~ IQ*COMB + SES + class, data = nl1,
           singular.ok = T), cor = F)

nl2 <- cbind(aggregate(nl1[c(1,7)], list(class = nl1$class), mean),
             unique(nl1[c("class", "COMB", "GS")]))
summary(lm(lang ~ IQave + COMB, data = nl2, weights = GS),
        cor = F)

sitka.lme <- lme(size ~ treat*ordered(Time),
   random = ~1 | tree, data = Sitka, method = "ML")
Sitka <- Sitka  # make a local copy for S-PLUS
attach(Sitka)
Sitka$treatslope <- Time * (treat == "ozone")
detach()
sitka.lme2 <- update(sitka.lme,
    fixed = size ~ ordered(Time) + treat + treatslope)
anova(sitka.lme, sitka.lme2)

# fitted curves
matrix(fitted(sitka.lme2, level = 0)[c(301:305, 1:5)],
       2, 5, byrow = T,
       dimnames = list(c("control", "ozone"), unique(Sitka$Time)))


# 10.2  Classic nested designs

if(F) {
summary(raov(Conc ~ Lab/Bat, data = coop, subset = Spc=="S1"))

is.random(coop) <- T
is.random(coop$Spc) <- F
is.random(coop)

varcomp(Conc ~ Lab/Bat, data = coop, subset = Spc=="S1")

varcomp(Conc ~ Lab/Bat, data = coop, subset = Spc=="S1",
        method = c("winsor", "minque0"))
}

#oats <- oats  # make a local copy: needed in S-PLUS
oats$Nf <- ordered(oats$N, levels = sort(levels(oats$N)))

oats.aov <- aov(Y ~ Nf*V + Error(B/V), data = oats, qr = T)
summary(oats.aov)
summary(oats.aov, split = list(Nf = list(L = 1, Dev = 2:3)))

plot(fitted(oats.aov[[4]]), studres(oats.aov[[4]]))
abline(h = 0, lty = 2)
oats.pr <- proj(oats.aov)
qqnorm(oats.pr[[4]][,"Residuals"], ylab = "Stratum 4 residuals")
qqline(oats.pr[[4]][,"Residuals"])

oats.aov <- aov(Y ~ N + V + Error(B/V), data = oats, qr = T)
model.tables(oats.aov, type = "means", se = T)
# we can get the unimplemented standard errors from
se.contrast(oats.aov, list(N == "0.0cwt", N == "0.2cwt"), data=oats)
se.contrast(oats.aov, list(V == "Golden.rain", V == "Victory"), data=oats)


# is.random(oats$B) <- T
# varcomp(Y ~ N + V + B/V, data = oats)

lme(Conc ~ 1, random = ~1 | Lab/Bat, data = coop,
    subset = Spc=="S1")

options(contrasts = c("contr.treatment", "contr.poly"))
summary(lme(Y ~ N + V, random = ~1 | B/V, data = oats))


# 10.3 Non-linear mixed effects models

options(contrasts = c("contr.treatment", "contr.poly"))
sitka.nlme <- nlme(size ~ A + B * (1 - exp(-(Time-100)/C)),
    fixed = list(A ~ treat, B ~ treat, C ~ 1),
    random = A + B ~ 1 | tree, data = Sitka,
    start  = list(fixed = c(2, 0, 4, 0, 100)), verbose = T)

summary(sitka.nlme)

summary(update(sitka.nlme,
               corr = corCAR1(0.95, ~Time | tree)))

Fpl <- deriv(~ A + (B-A)/(1 + exp((log(d) - ld50)/th)),
    c("A","B","ld50","th"), function(d, A, B, ld50, th) {})

st <- coef(nls(BPchange ~ Fpl(Dose, A, B, ld50, th),
          start = c(A = 25, B = 0, ld50 = 4, th = 0.25),
          data = Rabbit))
Rc.nlme <- nlme(BPchange ~ Fpl(Dose, A, B, ld50, th),
     fixed = list(A ~ 1, B ~ 1, ld50 ~ 1, th ~ 1),
     random = A + ld50 ~ 1 | Animal, data = Rabbit,
     subset = Treatment == "Control",
     start = list(fixed = st))
## The next fails on some R platforms and some versions of nlme
## Rm.nlme <- update(Rc.nlme, subset = Treatment=="MDL")
## so update starting values
st <- coef(nls(BPchange ~ Fpl(Dose, A, B, ld50, th),
          start = c(A = 25, B = 0, ld50 = 4, th = 0.25),
          data = Rabbit, subset = Treatment == "MDL"))
Rm.nlme <- update(Rc.nlme, subset = Treatment=="MDL",
                  start = list(fixed = st))

Rc.nlme
Rm.nlme

c1 <- c(28, 1.6, 4.1, 0.27, 0)
R.nlme1 <- nlme(BPchange ~ Fpl(Dose, A, B, ld50, th),
    fixed = list(A ~ Treatment, B ~ Treatment,
                 ld50 ~ Treatment, th ~ Treatment),
    random =  A + ld50 ~ 1 | Animal/Run, data = Rabbit,
    start = list(fixed = c1[c(1, 5, 2, 5, 3, 5, 4, 5)]))
summary(R.nlme1)
R.nlme2 <- update(R.nlme1,
     fixed = list(A ~ 1, B ~ 1, ld50 ~ Treatment, th ~ 1),
     start = list(fixed = c1[c(1:3, 5, 4)]))
anova(R.nlme2, R.nlme1)
summary(R.nlme2)

xyplot(BPchange ~ log(Dose) | Animal * Treatment, Rabbit,
    xlab = "log(Dose) of Phenylbiguanide",
    ylab = "Change in blood pressure (mm Hg)",
    subscripts = T, aspect = "xy", panel =
       function(x, y, subscripts) {
          panel.grid()
          panel.xyplot(x, y)
          sp <- spline(x, fitted(R.nlme2)[subscripts])
          panel.xyplot(sp$x, sp$y, type = "l")
       })


# 10.4 Generalized linear mixed models

# bacteria <- bacteria # needed in S-PLUS
contrasts(bacteria$trt) <- structure(contr.sdif(3),
    dimnames = list(NULL, c("drug", "encourage")))
summary(glm(y ~ trt * week, binomial, data = bacteria),
        cor = F)
summary(glm(y ~ trt + week, binomial, data = bacteria),
        cor = F)

summary(glm(y ~ lbase*trt + lage + V4, family = poisson,
            data = epil), cor = F)

# epil <- epil # needed in S-PLUS
epil2 <- epil[epil$period == 1, ]
epil2["period"] <- rep(0, 59); epil2["y"] <- epil2["base"]
epil["time"] <- 1; epil2["time"] <- 4
epil2 <- rbind(epil, epil2)
epil2$pred <- unclass(epil2$trt) * (epil2$period > 0)
epil2$subject <- factor(epil2$subject)
epil3 <- aggregate(epil2, list(epil2$subject, epil2$period > 0),
                   function(x) if(is.numeric(x)) sum(x) else x[1])
epil3$pred <- factor(epil3$pred, labels = c("base", "placebo", "drug"))

contrasts(epil3$pred) <- structure(contr.sdif(3),
    dimnames = list(NULL, c("placebo-base", "drug-placebo")))
summary(glm(y ~ pred + factor(subject) + offset(log(time)),
            family = poisson, data = epil3), cor = F)

glm(y ~ factor(subject), family = poisson, data = epil)

library(survival)
bacteria$Time <- rep(1, nrow(bacteria))
coxph(Surv(Time, unclass(y)) ~ week + strata(ID),
      data = bacteria, method = "exact")
coxph(Surv(Time, unclass(y)) ~ factor(week) + strata(ID),
      data = bacteria, method = "exact")
coxph(Surv(Time, unclass(y)) ~ I(week > 2) + strata(ID),
      data = bacteria, method = "exact")

fit <- glm(y ~ trt + I(week> 2), binomial, data = bacteria)
summary(fit, cor = F)
sum(residuals(fit, type = "pearson")^2)

if(F) { # very slow
library(GLMMGibbs)
# declare a random intercept for each subject
epil$subject <- Ra(data = factor(epil$subject))
glmm(y ~ lbase*trt + lage + V4 + subject, family = poisson,
     data = epil, keep = 100000, thin = 100)

epil3$subject <- Ra(data = factor(epil3$subject))
glmm(y ~ pred + subject, family = poisson,
     data = epil3, keep = 100000, thin = 100)
}


summary(glmmPQL(y ~ trt + I(week> 2), random = ~ 1 | ID,
                family = binomial, data = bacteria))
summary(glmmPQL(y ~ lbase*trt + lage + V4,
                random = ~ 1 | subject,
                family = poisson, data = epil))
summary(glmmPQL(y ~ pred, random = ~1 | subject,
                family = poisson, data = epil3))


# 10.5  GEE models

## modified for YAGS 3.21-3
library(yags)
attach(bacteria)
yags(y == "y" ~ trt + I(week > 2), family = binomial, alphainit=0,
     id = ID, corstr = "exchangeable")
detach("bacteria")

attach(epil)
yags(y ~ lbase*trt + lage + V4, family = poisson, alphainit=0,
     id = subject, corstr = "exchangeable")
detach("epil")

options(contrasts = c("contr.sum", "contr.poly"))
library(gee)
summary(gee(y ~ pred + factor(subject), family = poisson,
            id = subject, data = epil3,  corstr = "exchangeable"))

# End of ch10
