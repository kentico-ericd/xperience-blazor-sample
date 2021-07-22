"use strict";

var gulp = require("gulp");
var sass = require('gulp-sass')(require('node-sass'));

var cleanCSS = require("gulp-clean-css");
var concat = require("gulp-concat");
var autoprefixer = require("gulp-autoprefixer");
var del = require('del');

var projectPath = "./";
var wwwRootPath = "./wwwroot/";

gulp.task("clean", function () {
    return del([
        wwwRootPath + "css/style.css",
    ]);
});

gulp.task("sass", function () {
	return gulp.src(
	[
        wwwRootPath + "css/site.scss",
		projectPath + "Components/**/*.scss"
	])
	.pipe(sass())
	.pipe(concat("style.css"))
	.pipe(autoprefixer("last 2 version", "ie11", "safari 10"))
	.pipe(cleanCSS({ compatibility: "ie11", keepSpecialComments: "0" }))
	.pipe(gulp.dest(wwwRootPath + "css"));
});

gulp.task("default", gulp.series('clean', 'sass'));