(this["webpackJsonp@banana-cake-pop/main"] =
  this["webpackJsonp@banana-cake-pop/main"] || []).push([
  [6],
  {
    179: function (e, n, t) {
      "use strict";
      var r = t(261);
      t.o(r, "LogLevel") &&
        t.d(n, "LogLevel", function () {
          return r.LogLevel;
        });
      var i = t(262);
      t.o(i, "LogLevel") &&
        t.d(n, "LogLevel", function () {
          return i.LogLevel;
        });
      var a = t(263);
      t.o(a, "LogLevel") &&
        t.d(n, "LogLevel", function () {
          return a.LogLevel;
        });
      var o = t(264);
      t.o(o, "LogLevel") &&
        t.d(n, "LogLevel", function () {
          return o.LogLevel;
        });
      var c = t(265);
      t.o(c, "LogLevel") &&
        t.d(n, "LogLevel", function () {
          return c.LogLevel;
        });
      var s = t(266);
      t.o(s, "LogLevel") &&
        t.d(n, "LogLevel", function () {
          return s.LogLevel;
        });
      var u = t(267);
      t.d(n, "LogLevel", function () {
        return u.a;
      });
      t(268), t(269), t(270), t(271), t(272), t(273);
    },
    261: function (e, n) {},
    262: function (e, n) {},
    263: function (e, n) {},
    264: function (e, n) {},
    265: function (e, n) {},
    266: function (e, n) {},
    267: function (e, n, t) {
      "use strict";
      var r;
      t.d(n, "a", function () {
        return r;
      }),
        (function (e) {
          (e[(e.error = 0)] = "error"),
            (e[(e.warning = 1)] = "warning"),
            (e[(e.info = 2)] = "info");
        })(r || (r = {}));
    },
    268: function (e, n) {},
    269: function (e, n) {},
    270: function (e, n) {},
    271: function (e, n) {},
    272: function (e, n) {},
    273: function (e, n) {},
    274: function (e, n, t) {
      "use strict";
      t.d(n, "a", function () {
        return a;
      });
      var r = t(59);
      function i(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      var a = function e(n) {
        !(function (e, n) {
          if (!(e instanceof n))
            throw new TypeError("Cannot call a class as a function");
        })(this, e),
          i(this, "id", void 0),
          i(this, "type", void 0),
          i(this, "createdAt", void 0),
          i(this, "payload", void 0),
          (this.id = n.id || Object(r.b)()),
          (this.type = n.type),
          (this.createdAt = n.createdAt || Object(r.e)()),
          (this.payload = n.payload);
      };
    },
    275: function (e, n) {},
    276: function (e, n, t) {
      "use strict";
      t(277),
        t(278),
        t(279),
        t(280),
        t(281),
        t(282),
        t(283),
        t(284),
        t(285),
        t(286),
        t(287),
        t(288),
        t(289),
        t(290);
    },
    277: function (e, n) {},
    278: function (e, n) {},
    279: function (e, n) {},
    280: function (e, n) {},
    281: function (e, n) {},
    282: function (e, n) {},
    283: function (e, n) {},
    284: function (e, n) {},
    285: function (e, n) {},
    286: function (e, n) {},
    287: function (e, n) {},
    288: function (e, n) {},
    289: function (e, n) {},
    290: function (e, n) {},
    359: function (e, n) {},
    360: function (e, n) {},
    57: function (e, n, t) {
      "use strict";
      var r = t(274);
      t.d(n, "Message", function () {
        return r.a;
      });
      t(275), t(276);
    },
    59: function (e, n, t) {
      "use strict";
      t.d(n, "a", function () {
        return a;
      }),
        t.d(n, "b", function () {
          return c;
        }),
        t.d(n, "d", function () {
          return s;
        }),
        t.d(n, "e", function () {
          return d;
        }),
        t.d(n, "c", function () {
          return u;
        });
      var r = t(313),
        i = t.n(r);
      function a() {
        for (
          var e = new i.a(), n = arguments.length, t = new Array(n), r = 0;
          r < n;
          r++
        )
          t[r] = arguments[r];
        return (
          t.forEach(function (n) {
            return (e = e.update(n));
          }),
          e.digest("hex")
        );
      }
      var o = t(257);
      function c(e) {
        return e && e.length > 0
          ? "".concat(e, "|").concat(Object(o.a)())
          : Object(o.a)();
      }
      function s(e) {
        return "OperationDefinition" === e.kind;
      }
      function u(e, n) {
        if (e > n) throw new Error("'start' must be smaller than 'end'.");
        return n - e;
      }
      function d() {
        return Date.now();
      }
    },
    842: function (e, n, t) {
      "use strict";
      t.r(n);
      var r = t(59),
        i = t(932),
        a = t(933),
        o = t(934),
        c = t(57),
        s = t(936),
        u = t(935),
        d = t(8),
        p = t.n(d);
      function l(e, n, t, r, i, a, o) {
        try {
          var c = e[a](o),
            s = c.value;
        } catch (u) {
          return void t(u);
        }
        c.done ? n(s) : Promise.resolve(s).then(r, i);
      }
      function f(e, n) {
        for (var t = 0; t < n.length; t++) {
          var r = n[t];
          (r.enumerable = r.enumerable || !1),
            (r.configurable = !0),
            "value" in r && (r.writable = !0),
            Object.defineProperty(e, r.key, r);
        }
      }
      function m(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      var v = [0, 1, 1, 2, 3, 5, 8, 13, 21, 34],
        y = (function () {
          function e(n) {
            !(function (e, n) {
              if (!(e instanceof n))
                throw new TypeError("Cannot call a class as a function");
            })(this, e),
              (this.execute = n),
              m(this, "count", 0),
              m(this, "running", !1),
              m(this, "timeout", void 0);
          }
          var n, t, r;
          return (
            (n = e),
            (t = [
              {
                key: "start",
                value: function (e) {
                  var n = this,
                    t = (function () {
                      var r,
                        i =
                          ((r = p.a.mark(function r() {
                            var i;
                            return p.a.wrap(
                              function (r) {
                                for (;;)
                                  switch ((r.prev = r.next)) {
                                    case 0:
                                      return (
                                        (n.running = !0),
                                        (r.prev = 1),
                                        (r.next = 4),
                                        n.execute(n.count++)
                                      );
                                    case 4:
                                      if (!r.sent) {
                                        r.next = 7;
                                        break;
                                      }
                                      n.stop(), e();
                                    case 7:
                                      r.next = 12;
                                      break;
                                    case 9:
                                      (r.prev = 9),
                                        (r.t0 = r.catch(1)),
                                        console.error(r.t0);
                                    case 12:
                                      n.running &&
                                        (n.count < 10
                                          ? ((i = Math.floor(
                                              Math.random() * v[n.count] * 250
                                            )),
                                            (n.timeout = self.setTimeout(t, i)))
                                          : e());
                                    case 13:
                                    case "end":
                                      return r.stop();
                                  }
                              },
                              r,
                              null,
                              [[1, 9]]
                            );
                          })),
                          function () {
                            var e = this,
                              n = arguments;
                            return new Promise(function (t, i) {
                              var a = r.apply(e, n);
                              function o(e) {
                                l(a, t, i, o, c, "next", e);
                              }
                              function c(e) {
                                l(a, t, i, o, c, "throw", e);
                              }
                              o(void 0);
                            });
                          });
                      return function () {
                        return i.apply(this, arguments);
                      };
                    })();
                  t();
                },
              },
              {
                key: "stop",
                value: function () {
                  (this.running = !1), self.clearTimeout(this.timeout);
                },
              },
            ]) && f(n.prototype, t),
            r && f(n, r),
            e
          );
        })();
      function h(e) {
        var n = [];
        return (
          e &&
            e.definitions.forEach(function (e) {
              var t;
              Object(r.d)(e) &&
                (null === (t = e.name) || void 0 === t ? void 0 : t.value) &&
                n.push({ name: e.name.value, type: e.operation });
            }),
          n
        );
      }
      function b(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function g(e) {
        for (var n = 1; n < arguments.length; n++) {
          var t = null != arguments[n] ? arguments[n] : {};
          n % 2
            ? b(Object(t), !0).forEach(function (n) {
                k(e, n, t[n]);
              })
            : Object.getOwnPropertyDescriptors
            ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t))
            : b(Object(t)).forEach(function (n) {
                Object.defineProperty(
                  e,
                  n,
                  Object.getOwnPropertyDescriptor(t, n)
                );
              });
        }
        return e;
      }
      function k(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      function O(e, n) {
        return (
          (function (e) {
            if (Array.isArray(e)) return e;
          })(e) ||
          (function (e, n) {
            if (
              "undefined" === typeof Symbol ||
              !(Symbol.iterator in Object(e))
            )
              return;
            var t = [],
              r = !0,
              i = !1,
              a = void 0;
            try {
              for (
                var o, c = e[Symbol.iterator]();
                !(r = (o = c.next()).done) &&
                (t.push(o.value), !n || t.length !== n);
                r = !0
              );
            } catch (s) {
              (i = !0), (a = s);
            } finally {
              try {
                r || null == c.return || c.return();
              } finally {
                if (i) throw a;
              }
            }
            return t;
          })(e, n) ||
          (function (e, n) {
            if (!e) return;
            if ("string" === typeof e) return w(e, n);
            var t = Object.prototype.toString.call(e).slice(8, -1);
            "Object" === t && e.constructor && (t = e.constructor.name);
            if ("Map" === t || "Set" === t) return Array.from(e);
            if (
              "Arguments" === t ||
              /^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(t)
            )
              return w(e, n);
          })(e, n) ||
          (function () {
            throw new TypeError(
              "Invalid attempt to destructure non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method."
            );
          })()
        );
      }
      function w(e, n) {
        (null == n || n > e.length) && (n = e.length);
        for (var t = 0, r = new Array(n); t < n; t++) r[t] = e[t];
        return r;
      }
      var S = new i.a({});
      function j() {
        Ln("document-cache-delete-item", function (e, n) {
          !(function (e) {
            S.next(g(g({}, S.value), {}, k({}, e, void 0)));
          })(n.session.documentId);
        }),
          Ln("document-cache-set-item", function (e, n) {
            var t = e.payload;
            !(function (e, n) {
              S.next(g(g({}, S.value), {}, k({}, e, n)));
            })(n.session.documentId, t);
          });
      }
      function x(e) {
        return S.value[e];
      }
      function N(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function T(e, n) {
        for (var t = 0; t < n.length; t++) {
          var r = n[t];
          (r.enumerable = r.enumerable || !1),
            (r.configurable = !0),
            "value" in r && (r.writable = !0),
            Object.defineProperty(e, r.key, r);
        }
      }
      function P(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      var F = (function () {
        function e(n) {
          !(function (e, n) {
            if (!(e instanceof n))
              throw new TypeError("Cannot call a class as a function");
          })(this, e),
            (this.port = n),
            P(this, "id", Object(r.b)("context")),
            P(
              this,
              "session$",
              new i.a({
                documentId: "",
                httpMethod: "POST",
                schemaEndpoint: "",
              })
            ),
            P(this, "subscriptions", []),
            this.initialize();
        }
        var n, t, d;
        return (
          (n = e),
          (t = [
            {
              key: "dispose",
              value: function () {
                this.session$.complete(),
                  this.subscriptions.forEach(function (e) {
                    return e.unsubscribe();
                  });
              },
            },
            {
              key: "initialize",
              value: function () {
                var e,
                  n = this;
                Ln("set-session", function (e) {
                  var t = e.payload;
                  n.session$.next(
                    (function (e) {
                      for (var n = 1; n < arguments.length; n++) {
                        var t = null != arguments[n] ? arguments[n] : {};
                        n % 2
                          ? N(Object(t), !0).forEach(function (n) {
                              P(e, n, t[n]);
                            })
                          : Object.getOwnPropertyDescriptors
                          ? Object.defineProperties(
                              e,
                              Object.getOwnPropertyDescriptors(t)
                            )
                          : N(Object(t)).forEach(function (n) {
                              Object.defineProperty(
                                e,
                                n,
                                Object.getOwnPropertyDescriptor(t, n)
                              );
                            });
                      }
                      return e;
                    })({}, t)
                  );
                }),
                  this.subscriptions.push(
                    ((e = this),
                    Object(s.a)([
                      e.select(function (e) {
                        return e.documentId;
                      }),
                      S,
                    ])
                      .pipe(
                        Object(a.a)(function (e) {
                          var n = O(e, 2),
                            t = n[0];
                          return { documentId: t, document: n[1][t] };
                        }),
                        Object(o.a)(),
                        Object(u.a)(function (e) {
                          var n = e.document,
                            t = e.documentId;
                          return !!n && "" !== t;
                        })
                      )
                      .subscribe(function (n) {
                        var t = n.document;
                        e.sendMessage(
                          new c.Message({
                            type: "document-operations-update",
                            payload: h(t),
                          })
                        );
                      }))
                  );
              },
            },
            {
              key: "select",
              value: function (e) {
                return this.session$.pipe(Object(a.a)(e), Object(o.a)());
              },
            },
            {
              key: "sendMessage",
              value: function (e) {
                this.port.postMessage(e);
              },
            },
            {
              key: "session",
              get: function () {
                return this.session$.value;
              },
            },
          ]) && T(n.prototype, t),
          d && T(n, d),
          e
        );
      })();
      function E(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function I(e) {
        for (var n = 1; n < arguments.length; n++) {
          var t = null != arguments[n] ? arguments[n] : {};
          n % 2
            ? E(Object(t), !0).forEach(function (n) {
                D(e, n, t[n]);
              })
            : Object.getOwnPropertyDescriptors
            ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t))
            : E(Object(t)).forEach(function (n) {
                Object.defineProperty(
                  e,
                  n,
                  Object.getOwnPropertyDescriptor(t, n)
                );
              });
        }
        return e;
      }
      function D(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      var M = {};
      function L() {
        Ln("http-headers-cache-delete-item", function (e, n) {
          !(function (e) {
            M = I(I({}, M), {}, D({}, e, void 0));
          })(n.session.documentId);
        }),
          Ln("http-headers-cache-set-item", function (e, n) {
            var t = e.payload;
            !(function (e, n) {
              M = I(I({}, M), {}, D({}, e, n));
            })(n.session.documentId, t);
          });
      }
      function _(e) {
        return M[e];
      }
      var R = t(336),
        C = t(939),
        V = t(334);
      function q(e, n, t, r, i, a, o) {
        try {
          var c = e[a](o),
            s = c.value;
        } catch (u) {
          return void t(u);
        }
        c.done ? n(s) : Promise.resolve(s).then(r, i);
      }
      function G(e) {
        return function () {
          var n = this,
            t = arguments;
          return new Promise(function (r, i) {
            var a = e.apply(n, t);
            function o(e) {
              q(a, r, i, o, c, "next", e);
            }
            function c(e) {
              q(a, r, i, o, c, "throw", e);
            }
            o(void 0);
          });
        };
      }
      function A(e) {
        return Q.apply(this, arguments);
      }
      function Q() {
        return (Q = G(
          p.a.mark(function e(n) {
            var t;
            return p.a.wrap(function (e) {
              for (;;)
                switch ((e.prev = e.next)) {
                  case 0:
                    return (e.next = 2), B();
                  case 2:
                    return (t = e.sent), (e.next = 5), t.get("schema", n);
                  case 5:
                    return e.abrupt("return", e.sent);
                  case 6:
                  case "end":
                    return e.stop();
                }
            }, e);
          })
        )).apply(this, arguments);
      }
      function J(e, n) {
        return $.apply(this, arguments);
      }
      function $() {
        return ($ = G(
          p.a.mark(function e(n, t) {
            var r;
            return p.a.wrap(function (e) {
              for (;;)
                switch ((e.prev = e.next)) {
                  case 0:
                    return (e.next = 2), B();
                  case 2:
                    return (r = e.sent), (e.next = 5), r.put("schema", t, n);
                  case 5:
                  case "end":
                    return e.stop();
                }
            }, e);
          })
        )).apply(this, arguments);
      }
      function B() {
        return Object(V.a)("bcp-schema", 1, {
          upgrade: function (e) {
            e.createObjectStore("document"), e.createObjectStore("schema");
          },
        });
      }
      function U(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function z(e) {
        for (var n = 1; n < arguments.length; n++) {
          var t = null != arguments[n] ? arguments[n] : {};
          n % 2
            ? U(Object(t), !0).forEach(function (n) {
                K(e, n, t[n]);
              })
            : Object.getOwnPropertyDescriptors
            ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t))
            : U(Object(t)).forEach(function (n) {
                Object.defineProperty(
                  e,
                  n,
                  Object.getOwnPropertyDescriptor(t, n)
                );
              });
        }
        return e;
      }
      function K(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      function H(e, n, t, r, i, a, o) {
        try {
          var c = e[a](o),
            s = c.value;
        } catch (u) {
          return void t(u);
        }
        c.done ? n(s) : Promise.resolve(s).then(r, i);
      }
      function W(e) {
        return function () {
          var n = this,
            t = arguments;
          return new Promise(function (r, i) {
            var a = e.apply(n, t);
            function o(e) {
              H(a, r, i, o, c, "next", e);
            }
            function c(e) {
              H(a, r, i, o, c, "throw", e);
            }
            o(void 0);
          });
        };
      }
      var X = {};
      function Y(e) {
        return Z.apply(this, arguments);
      }
      function Z() {
        return (Z = W(
          p.a.mark(function e(n) {
            var t, r, i;
            return p.a.wrap(function (e) {
              for (;;)
                switch ((e.prev = e.next)) {
                  case 0:
                    if (((t = n.session.schemaEndpoint), !(r = X[t]))) {
                      e.next = 4;
                      break;
                    }
                    return e.abrupt("return", r);
                  case 4:
                    return (e.next = 6), A(t);
                  case 6:
                    return (
                      (i = e.sent) && te(n, t, i.schema, i.hash),
                      e.abrupt("return", X[t])
                    );
                  case 9:
                  case "end":
                    return e.stop();
                }
            }, e);
          })
        )).apply(this, arguments);
      }
      function ee(e, n) {
        return ne.apply(this, arguments);
      }
      function ne() {
        return (ne = W(
          p.a.mark(function e(n, t) {
            var i, a, o;
            return p.a.wrap(function (e) {
              for (;;)
                switch ((e.prev = e.next)) {
                  case 0:
                    return (i = n.session.schemaEndpoint), (e.next = 3), Y(n);
                  case 3:
                    if (
                      ((a = e.sent),
                      (o = Object(r.a)(JSON.stringify(t))),
                      a && a.hash === o)
                    ) {
                      e.next = 9;
                      break;
                    }
                    return (
                      te(n, i, t, o), (e.next = 9), J(i, { hash: o, schema: t })
                    );
                  case 9:
                  case "end":
                    return e.stop();
                }
            }, e);
          })
        )).apply(this, arguments);
      }
      function te(e, n, t, r) {
        var i = Object(C.a)(t);
        se(e, {
          endpoint: n,
          exists: !0,
          hash: r,
          hasMutationType: !!i.getMutationType(),
          hasSubscriptionType: !!i.getSubscriptionType(),
        }),
          (X = z(z({}, X), {}, K({}, n, { hash: r, schema: i })));
      }
      function re(e, n, t, r, i, a, o) {
        try {
          var c = e[a](o),
            s = c.value;
        } catch (u) {
          return void t(u);
        }
        c.done ? n(s) : Promise.resolve(s).then(r, i);
      }
      function ie(e) {
        return function () {
          var n = this,
            t = arguments;
          return new Promise(function (r, i) {
            var a = e.apply(n, t);
            function o(e) {
              re(a, r, i, o, c, "next", e);
            }
            function c(e) {
              re(a, r, i, o, c, "throw", e);
            }
            o(void 0);
          });
        };
      }
      var ae = {
        "GraphQL: Validation": "validation",
        "GraphQL: Deprecation": "deprecation",
        "GraphQL: Syntax": "syntax",
      };
      function oe() {
        Ln(
          "auto-suggestions",
          (function () {
            var e = ie(
              p.a.mark(function e(n, t) {
                var r, i, a, o, s, u, d, l;
                return p.a.wrap(function (e) {
                  for (;;)
                    switch ((e.prev = e.next)) {
                      case 0:
                        return (
                          (r = n.id),
                          (i = n.payload),
                          (a = i.content),
                          (o = i.cursor),
                          (s = n.type),
                          (e.next = 3),
                          Y(t)
                        );
                      case 3:
                        (u = e.sent),
                          (d = []),
                          u &&
                            ((l = Object(R.a)(u.schema, a, o)),
                            (d = l.map(function (e) {
                              var n = e.label;
                              return {
                                label: n,
                                kind: e.kind,
                                detail: e.detail,
                                documentation: e.documentation,
                                insertText: n,
                              };
                            }))),
                          t.sendMessage(
                            new c.Message({ id: r, type: s, payload: d })
                          );
                      case 7:
                      case "end":
                        return e.stop();
                    }
                }, e);
              })
            );
            return function (n, t) {
              return e.apply(this, arguments);
            };
          })()
        ),
          Ln(
            "diagnostics",
            (function () {
              var e = ie(
                p.a.mark(function e(n, t) {
                  var r, i, a, o, s, u;
                  return p.a.wrap(function (e) {
                    for (;;)
                      switch ((e.prev = e.next)) {
                        case 0:
                          return (
                            (r = n.id),
                            (i = n.payload),
                            (a = n.type),
                            (e.next = 3),
                            Y(t)
                          );
                        case 3:
                          (o = e.sent),
                            (s = Object(R.b)(i || "", o ? o.schema : void 0)),
                            (u = s.map(function (e) {
                              var n = e.message,
                                t = e.range,
                                r = e.severity,
                                i = e.source;
                              return {
                                message: n,
                                severity: r ? ce(r) : 1,
                                source: i ? ae[i] : void 0,
                                startLineNumber: t.start.line + 1,
                                startColumn: t.start.character + 1,
                                endLineNumber: t.end.line + 1,
                                endColumn: t.end.character + 1,
                              };
                            })),
                            t.sendMessage(
                              new c.Message({ id: r, type: a, payload: u })
                            );
                        case 7:
                        case "end":
                          return e.stop();
                      }
                  }, e);
                })
              );
              return function (n, t) {
                return e.apply(this, arguments);
              };
            })()
          ),
          Ln(
            "hover-information",
            (function () {
              var e = ie(
                p.a.mark(function e(n, t) {
                  var r, i, a, o, s, u, d;
                  return p.a.wrap(function (e) {
                    for (;;)
                      switch ((e.prev = e.next)) {
                        case 0:
                          return (
                            (r = n.id),
                            (i = n.payload),
                            (a = i.content),
                            (o = i.cursor),
                            (s = n.type),
                            (e.next = 3),
                            Y(t)
                          );
                        case 3:
                          (u = e.sent),
                            (d = void 0),
                            u && (d = Object(R.c)(u.schema, a, o)),
                            t.sendMessage(
                              new c.Message({ id: r, type: s, payload: d })
                            );
                        case 7:
                        case "end":
                          return e.stop();
                      }
                  }, e);
                })
              );
              return function (n, t) {
                return e.apply(this, arguments);
              };
            })()
          );
      }
      function ce(e) {
        return [8, 4, 2, 1][e - 1];
      }
      function se(e, n) {
        e.sendMessage(new c.Message({ type: "schema-update", payload: n }));
      }
      var ue = t(179);
      function de(e, n, t, r) {
        var i = r || e.session.documentId;
        e.sendMessage(
          new c.Message({
            type: "log",
            payload: {
              tabId: i,
              level: ue.LogLevel.error,
              message: n,
              details: t,
            },
          })
        );
      }
      function pe(e, n, t, r) {
        var i = r || e.session.documentId;
        e.sendMessage(
          new c.Message({
            type: "log",
            payload: {
              tabId: i,
              level: ue.LogLevel.info,
              message: n,
              details: t,
            },
          })
        );
      }
      t(829);
      var le = t(93);
      function fe(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function me(e) {
        for (var n = 1; n < arguments.length; n++) {
          var t = null != arguments[n] ? arguments[n] : {};
          n % 2
            ? fe(Object(t), !0).forEach(function (n) {
                ve(e, n, t[n]);
              })
            : Object.getOwnPropertyDescriptors
            ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t))
            : fe(Object(t)).forEach(function (n) {
                Object.defineProperty(
                  e,
                  n,
                  Object.getOwnPropertyDescriptor(t, n)
                );
              });
        }
        return e;
      }
      function ve(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      function ye(e, n, t, r, i, a, o) {
        try {
          var c = e[a](o),
            s = c.value;
        } catch (u) {
          return void t(u);
        }
        c.done ? n(s) : Promise.resolve(s).then(r, i);
      }
      function he(e) {
        return function () {
          var n = this,
            t = arguments;
          return new Promise(function (r, i) {
            var a = e.apply(n, t);
            function o(e) {
              ye(a, r, i, o, c, "next", e);
            }
            function c(e) {
              ye(a, r, i, o, c, "throw", e);
            }
            o(void 0);
          });
        };
      }
      var be = {
        kind: "Document",
        definitions: [
          {
            kind: "OperationDefinition",
            operation: "query",
            name: { kind: "Name", value: "introspection_phase_1" },
            variableDefinitions: [],
            directives: [],
            selectionSet: {
              kind: "SelectionSet",
              selections: [
                {
                  kind: "Field",
                  alias: { kind: "Name", value: "schema" },
                  name: { kind: "Name", value: "__type" },
                  arguments: [
                    {
                      kind: "Argument",
                      name: { kind: "Name", value: "name" },
                      value: {
                        kind: "StringValue",
                        value: "__Schema",
                        block: !1,
                      },
                    },
                  ],
                  directives: [],
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "name" },
                        arguments: [],
                        directives: [],
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "fields" },
                        arguments: [],
                        directives: [],
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                              arguments: [],
                              directives: [],
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: "Field",
                  alias: { kind: "Name", value: "directive" },
                  name: { kind: "Name", value: "__type" },
                  arguments: [
                    {
                      kind: "Argument",
                      name: { kind: "Name", value: "name" },
                      value: {
                        kind: "StringValue",
                        value: "__Directive",
                        block: !1,
                      },
                    },
                  ],
                  directives: [],
                  selectionSet: {
                    kind: "SelectionSet",
                    selections: [
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "name" },
                        arguments: [],
                        directives: [],
                      },
                      {
                        kind: "Field",
                        name: { kind: "Name", value: "fields" },
                        arguments: [],
                        directives: [],
                        selectionSet: {
                          kind: "SelectionSet",
                          selections: [
                            {
                              kind: "Field",
                              name: { kind: "Name", value: "name" },
                              arguments: [],
                              directives: [],
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
        loc: {
          start: 0,
          end: 199,
          source: {
            body:
              'query introspection_phase_1 {\n  schema: __type(name: "__Schema") {\n    name\n    fields {\n      name\n    }\n  }\n\n  directive: __type(name: "__Directive") {\n    name\n    fields {\n      name\n    }\n  }\n}\n',
            name: "GraphQL request",
            locationOffset: { line: 1, column: 1 },
          },
        },
      };
      function ge(e, n) {
        return ke.apply(this, arguments);
      }
      function ke() {
        return (ke = he(
          p.a.mark(function e(n, t) {
            var r, i, a, o, c, s;
            return p.a.wrap(function (e) {
              for (;;)
                switch ((e.prev = e.next)) {
                  case 0:
                    return (e.next = 2), Ne(n, t);
                  case 2:
                    if ((r = e.sent).response instanceof Response) {
                      e.next = 5;
                      break;
                    }
                    return e.abrupt(
                      "return",
                      me(me({}, r), {}, { response: { error: r.response } })
                    );
                  case 5:
                    return (e.next = 7), Te(r.response);
                  case 7:
                    if (((i = e.sent), (a = i.data), !(o = i.error))) {
                      e.next = 13;
                      break;
                    }
                    return (
                      (c = we(o, r.response)),
                      e.abrupt("return", me(me({}, r), {}, { response: c }))
                    );
                  case 13:
                    return (
                      (s = Oe(a, r.response)),
                      e.abrupt("return", me(me({}, r), {}, { response: s }))
                    );
                  case 15:
                  case "end":
                    return e.stop();
                }
            }, e);
          })
        )).apply(this, arguments);
      }
      function Oe(e, n) {
        return {
          parsedBody: e,
          headers: Se(n.headers),
          statusCode: n.status,
          statusText: n.statusText,
        };
      }
      function we(e, n) {
        return {
          error: e,
          headers: Se(n.headers),
          statusCode: n.status,
          statusText: n.statusText,
        };
      }
      function Se(e) {
        var n = Object.create(null);
        return (
          e.forEach(function (e, t) {
            n[t] = e;
          }),
          n
        );
      }
      function je(e) {
        return xe.apply(this, arguments);
      }
      function xe() {
        return (xe = he(
          p.a.mark(function e(n) {
            var t, r, i, a, o, c;
            return p.a.wrap(function (e) {
              for (;;)
                switch ((e.prev = e.next)) {
                  case 0:
                    return (
                      (e.next = 2),
                      Ne(n, {
                        query: be,
                        variables: {},
                        operationName: "introspection_phase_1",
                      })
                    );
                  case 2:
                    if ((t = e.sent).response instanceof Response) {
                      e.next = 5;
                      break;
                    }
                    return e.abrupt("return", t.response);
                  case 5:
                    return (e.next = 7), Te(t.response);
                  case 7:
                    if (!(r = e.sent).error) {
                      e.next = 10;
                      break;
                    }
                    return e.abrupt("return", r.error);
                  case 10:
                    return (
                      (i = Ie(r.data.data)),
                      (a = Fe(i)),
                      (e.next = 14),
                      Ne(n, {
                        query: a,
                        variables: {},
                        operationName: "introspection_phase_2",
                      })
                    );
                  case 14:
                    if ((o = e.sent).response instanceof Response) {
                      e.next = 17;
                      break;
                    }
                    return e.abrupt("return", o.response);
                  case 17:
                    return (e.next = 19), Te(o.response);
                  case 19:
                    if (!(c = e.sent).error) {
                      e.next = 22;
                      break;
                    }
                    return e.abrupt("return", c.error);
                  case 22:
                    return e.abrupt("return", c.data.data);
                  case 23:
                  case "end":
                    return e.stop();
                }
            }, e);
          })
        )).apply(this, arguments);
      }
      function Ne(e, n) {
        var t = e.credentials || "omit",
          r = n.extensions,
          i = n.operationName,
          a = n.variables,
          o = {
            cache: "no-cache",
            credentials: t,
            referrerPolicy: "no-referrer",
          };
        if (e.useGET) {
          var c = (function (e, n) {
            var t = [],
              r = function (e, n) {
                t.push("".concat(e, "=").concat(encodeURIComponent(n)));
              };
            n.query && r("query", Object(le.print)(n.query));
            n.operationName && r("operationName", n.operationName);
            n.variables && r("variables", JSON.stringify(n.variables));
            n.extensions && r("extensions", JSON.stringify(n.extensions));
            var i = "",
              a = e,
              o = e.indexOf("#");
            -1 !== o && ((i = e.substr(o)), (a = e.substr(0, o)));
            var c = -1 === a.indexOf("?") ? "?" : "&";
            return a + c + t.join("&") + i;
          })(e.uri, n);
          return self
            .fetch(c, me(me({}, o), {}, { method: "GET", headers: e.headers }))
            .catch(function (e) {
              return e instanceof TypeError
                ? Pe("Server Not Reachable", e.message)
                : Pe("Server Not Reachable");
            })
            .then(function (n) {
              return {
                request: {
                  uri: c,
                  method: "GET",
                  credentials: e.credentials,
                  cache: "no-cache",
                  referrerPolicy: "no-referrer",
                  headers: e.headers,
                },
                response: n,
              };
            });
        }
        var s = {
            extensions: r,
            operationName: i,
            query: Object(le.print)(n.query),
            variables: a,
          },
          u = JSON.stringify(s);
        return self
          .fetch(
            e.uri,
            me(
              me({}, o),
              {},
              {
                method: "POST",
                headers: me(
                  me({}, e.headers),
                  {},
                  { "Content-Type": "application/json" }
                ),
                body: u,
              }
            )
          )
          .catch(function (e) {
            return e instanceof TypeError
              ? Pe("Server Not Reachable", e.message)
              : Pe("Server Not Reachable");
          })
          .then(function (n) {
            return {
              request: {
                uri: e.uri,
                method: "POST",
                credentials: e.credentials,
                cache: "no-cache",
                referrerPolicy: "no-referrer",
                headers: me(
                  me({}, e.headers),
                  {},
                  { "Content-Type": "application/json" }
                ),
                body: s,
              },
              response: n,
            };
          });
      }
      function Te(e) {
        return e
          .text()
          .then(function (n) {
            return e.status >= 300
              ? (function (e, n, t) {
                  var r = new Error(t);
                  return (
                    (r.name = "ServerError"),
                    (r.statusCode = e.status),
                    (r.statusText = e.statusText),
                    (r.bodyText = n),
                    r
                  );
                })(
                  e,
                  n,
                  "Response not successful: Received status code ".concat(
                    e.status
                  )
                )
              : n;
          })
          .then(function (n) {
            if ("string" !== typeof n) return { error: n };
            try {
              return { data: JSON.parse(n) };
            } catch (t) {
              return {
                error: (function (e, n, t) {
                  var r = n;
                  return (
                    (r.name = "ServerParseError"),
                    (r.statusCode = e.status),
                    (r.statusText = e.statusText),
                    (r.bodyText = t),
                    r
                  );
                })(e, t, n),
              };
            }
          });
      }
      function Pe(e, n) {
        var t = new Error(e);
        return (t.name = "ServerNetworkError"), (t.statusText = n), t;
      }
      function Fe(e) {
        var n = {
            kind: "Document",
            definitions: [
              {
                kind: "OperationDefinition",
                operation: "query",
                name: { kind: "Name", value: "introspection_phase_2" },
                variableDefinitions: [],
                directives: [],
                selectionSet: {
                  kind: "SelectionSet",
                  selections: [
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "__schema" },
                      arguments: [],
                      directives: [],
                      selectionSet: {
                        kind: "SelectionSet",
                        selections: [
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "queryType" },
                            arguments: [],
                            directives: [],
                            selectionSet: {
                              kind: "SelectionSet",
                              selections: [
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "name" },
                                  arguments: [],
                                  directives: [],
                                },
                              ],
                            },
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "mutationType" },
                            arguments: [],
                            directives: [],
                            selectionSet: {
                              kind: "SelectionSet",
                              selections: [
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "name" },
                                  arguments: [],
                                  directives: [],
                                },
                              ],
                            },
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "subscriptionType" },
                            arguments: [],
                            directives: [],
                            selectionSet: {
                              kind: "SelectionSet",
                              selections: [
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "name" },
                                  arguments: [],
                                  directives: [],
                                },
                              ],
                            },
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "types" },
                            arguments: [],
                            directives: [],
                            selectionSet: {
                              kind: "SelectionSet",
                              selections: [
                                {
                                  kind: "FragmentSpread",
                                  name: { kind: "Name", value: "FullType" },
                                  directives: [],
                                },
                              ],
                            },
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "directives" },
                            arguments: [],
                            directives: [],
                            selectionSet: {
                              kind: "SelectionSet",
                              selections: [
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "name" },
                                  arguments: [],
                                  directives: [],
                                },
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "description" },
                                  arguments: [],
                                  directives: [],
                                },
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "args" },
                                  arguments: [],
                                  directives: [],
                                  selectionSet: {
                                    kind: "SelectionSet",
                                    selections: [
                                      {
                                        kind: "FragmentSpread",
                                        name: {
                                          kind: "Name",
                                          value: "InputValue",
                                        },
                                        directives: [],
                                      },
                                    ],
                                  },
                                },
                              ],
                            },
                          },
                        ],
                      },
                    },
                  ],
                },
              },
              {
                kind: "FragmentDefinition",
                name: { kind: "Name", value: "FullType" },
                typeCondition: {
                  kind: "NamedType",
                  name: { kind: "Name", value: "__Type" },
                },
                directives: [],
                selectionSet: {
                  kind: "SelectionSet",
                  selections: [
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "kind" },
                      arguments: [],
                      directives: [],
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "name" },
                      arguments: [],
                      directives: [],
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "description" },
                      arguments: [],
                      directives: [],
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "fields" },
                      arguments: [
                        {
                          kind: "Argument",
                          name: { kind: "Name", value: "includeDeprecated" },
                          value: { kind: "BooleanValue", value: !0 },
                        },
                      ],
                      directives: [],
                      selectionSet: {
                        kind: "SelectionSet",
                        selections: [
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "name" },
                            arguments: [],
                            directives: [],
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "description" },
                            arguments: [],
                            directives: [],
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "args" },
                            arguments: [],
                            directives: [],
                            selectionSet: {
                              kind: "SelectionSet",
                              selections: [
                                {
                                  kind: "FragmentSpread",
                                  name: { kind: "Name", value: "InputValue" },
                                  directives: [],
                                },
                              ],
                            },
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "type" },
                            arguments: [],
                            directives: [],
                            selectionSet: {
                              kind: "SelectionSet",
                              selections: [
                                {
                                  kind: "FragmentSpread",
                                  name: { kind: "Name", value: "TypeRef" },
                                  directives: [],
                                },
                              ],
                            },
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "isDeprecated" },
                            arguments: [],
                            directives: [],
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "deprecationReason" },
                            arguments: [],
                            directives: [],
                          },
                        ],
                      },
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "inputFields" },
                      arguments: [],
                      directives: [],
                      selectionSet: {
                        kind: "SelectionSet",
                        selections: [
                          {
                            kind: "FragmentSpread",
                            name: { kind: "Name", value: "InputValue" },
                            directives: [],
                          },
                        ],
                      },
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "interfaces" },
                      arguments: [],
                      directives: [],
                      selectionSet: {
                        kind: "SelectionSet",
                        selections: [
                          {
                            kind: "FragmentSpread",
                            name: { kind: "Name", value: "TypeRef" },
                            directives: [],
                          },
                        ],
                      },
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "enumValues" },
                      arguments: [
                        {
                          kind: "Argument",
                          name: { kind: "Name", value: "includeDeprecated" },
                          value: { kind: "BooleanValue", value: !0 },
                        },
                      ],
                      directives: [],
                      selectionSet: {
                        kind: "SelectionSet",
                        selections: [
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "name" },
                            arguments: [],
                            directives: [],
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "description" },
                            arguments: [],
                            directives: [],
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "isDeprecated" },
                            arguments: [],
                            directives: [],
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "deprecationReason" },
                            arguments: [],
                            directives: [],
                          },
                        ],
                      },
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "possibleTypes" },
                      arguments: [],
                      directives: [],
                      selectionSet: {
                        kind: "SelectionSet",
                        selections: [
                          {
                            kind: "FragmentSpread",
                            name: { kind: "Name", value: "TypeRef" },
                            directives: [],
                          },
                        ],
                      },
                    },
                  ],
                },
              },
              {
                kind: "FragmentDefinition",
                name: { kind: "Name", value: "InputValue" },
                typeCondition: {
                  kind: "NamedType",
                  name: { kind: "Name", value: "__InputValue" },
                },
                directives: [],
                selectionSet: {
                  kind: "SelectionSet",
                  selections: [
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "name" },
                      arguments: [],
                      directives: [],
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "description" },
                      arguments: [],
                      directives: [],
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "type" },
                      arguments: [],
                      directives: [],
                      selectionSet: {
                        kind: "SelectionSet",
                        selections: [
                          {
                            kind: "FragmentSpread",
                            name: { kind: "Name", value: "TypeRef" },
                            directives: [],
                          },
                        ],
                      },
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "defaultValue" },
                      arguments: [],
                      directives: [],
                    },
                  ],
                },
              },
              {
                kind: "FragmentDefinition",
                name: { kind: "Name", value: "TypeRef" },
                typeCondition: {
                  kind: "NamedType",
                  name: { kind: "Name", value: "__Type" },
                },
                directives: [],
                selectionSet: {
                  kind: "SelectionSet",
                  selections: [
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "kind" },
                      arguments: [],
                      directives: [],
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "name" },
                      arguments: [],
                      directives: [],
                    },
                    {
                      kind: "Field",
                      name: { kind: "Name", value: "ofType" },
                      arguments: [],
                      directives: [],
                      selectionSet: {
                        kind: "SelectionSet",
                        selections: [
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "kind" },
                            arguments: [],
                            directives: [],
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "name" },
                            arguments: [],
                            directives: [],
                          },
                          {
                            kind: "Field",
                            name: { kind: "Name", value: "ofType" },
                            arguments: [],
                            directives: [],
                            selectionSet: {
                              kind: "SelectionSet",
                              selections: [
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "kind" },
                                  arguments: [],
                                  directives: [],
                                },
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "name" },
                                  arguments: [],
                                  directives: [],
                                },
                                {
                                  kind: "Field",
                                  name: { kind: "Name", value: "ofType" },
                                  arguments: [],
                                  directives: [],
                                  selectionSet: {
                                    kind: "SelectionSet",
                                    selections: [
                                      {
                                        kind: "Field",
                                        name: { kind: "Name", value: "kind" },
                                        arguments: [],
                                        directives: [],
                                      },
                                      {
                                        kind: "Field",
                                        name: { kind: "Name", value: "name" },
                                        arguments: [],
                                        directives: [],
                                      },
                                      {
                                        kind: "Field",
                                        name: { kind: "Name", value: "ofType" },
                                        arguments: [],
                                        directives: [],
                                        selectionSet: {
                                          kind: "SelectionSet",
                                          selections: [
                                            {
                                              kind: "Field",
                                              name: {
                                                kind: "Name",
                                                value: "kind",
                                              },
                                              arguments: [],
                                              directives: [],
                                            },
                                            {
                                              kind: "Field",
                                              name: {
                                                kind: "Name",
                                                value: "name",
                                              },
                                              arguments: [],
                                              directives: [],
                                            },
                                            {
                                              kind: "Field",
                                              name: {
                                                kind: "Name",
                                                value: "ofType",
                                              },
                                              arguments: [],
                                              directives: [],
                                              selectionSet: {
                                                kind: "SelectionSet",
                                                selections: [
                                                  {
                                                    kind: "Field",
                                                    name: {
                                                      kind: "Name",
                                                      value: "kind",
                                                    },
                                                    arguments: [],
                                                    directives: [],
                                                  },
                                                  {
                                                    kind: "Field",
                                                    name: {
                                                      kind: "Name",
                                                      value: "name",
                                                    },
                                                    arguments: [],
                                                    directives: [],
                                                  },
                                                ],
                                              },
                                            },
                                          ],
                                        },
                                      },
                                    ],
                                  },
                                },
                              ],
                            },
                          },
                        ],
                      },
                    },
                  ],
                },
              },
            ],
            loc: {
              start: 0,
              end: 1138,
              source: {
                body:
                  "query introspection_phase_2 {\n  __schema {\n    queryType {\n      name\n    }\n    mutationType {\n      name\n    }\n    subscriptionType {\n      name\n    }\n    types {\n      ...FullType\n    }\n    directives {\n      name\n      description\n      args {\n        ...InputValue\n      }\n    }\n  }\n}\n\nfragment FullType on __Type {\n  kind\n  name\n  description\n  fields(includeDeprecated: true) {\n    name\n    description\n    args {\n      ...InputValue\n    }\n    type {\n      ...TypeRef\n    }\n    isDeprecated\n    deprecationReason\n  }\n  inputFields {\n    ...InputValue\n  }\n  interfaces {\n    ...TypeRef\n  }\n  enumValues(includeDeprecated: true) {\n    name\n    description\n    isDeprecated\n    deprecationReason\n  }\n  possibleTypes {\n    ...TypeRef\n  }\n}\n\nfragment InputValue on __InputValue {\n  name\n  description\n  type {\n    ...TypeRef\n  }\n  defaultValue\n}\n\nfragment TypeRef on __Type {\n  kind\n  name\n  ofType {\n    kind\n    name\n    ofType {\n      kind\n      name\n      ofType {\n        kind\n        name\n        ofType {\n          kind\n          name\n          ofType {\n            kind\n            name\n          }\n        }\n      }\n    }\n  }\n}\n",
                name: "GraphQL request",
                locationOffset: { line: 1, column: 1 },
              },
            },
          },
          t = n.definitions
            .find(function (e) {
              return "OperationDefinition" === e.kind;
            })
            .selectionSet.selections.find(function (e) {
              return "Field" === e.kind;
            });
        return (
          (function (e, n) {
            var t = [];
            e.selectionSet.selections.forEach(function (e) {
              return t.push(e);
            }),
              n.isRepeatable && t.push(Ee("isRepeatable"));
            n.locations
              ? t.push(Ee("locations"))
              : (t.push(Ee("onField")),
                t.push(Ee("onFragment")),
                t.push(Ee("onOperation")));
            e.selectionSet.selections = t;
          })(
            t.selectionSet.selections.find(function (e) {
              return "Field" === e.kind && "directives" === e.name.value;
            }),
            e
          ),
          (function (e, n) {
            if (!n.subscription) {
              var t = [];
              e.selectionSet.selections.forEach(function (e) {
                ("Field" === e.kind && "subscriptionType" === e.name.value) ||
                  t.push(e);
              }),
                (e.selectionSet.selections = t);
            }
          })(t, e),
          n
        );
      }
      function Ee(e) {
        return {
          kind: "Field",
          name: { kind: "Name", value: e },
          directives: [],
          arguments: [],
        };
      }
      function Ie(e) {
        var n = e.directive;
        return {
          isRepeatable: !!n.fields.find(function (e) {
            return "isRepeatable" === e.name;
          }),
          locations: !!n.fields.find(function (e) {
            return "locations" === e.name;
          }),
          subscription: !!e.schema.fields.find(function (e) {
            return "subscriptionType" === e.name;
          }),
        };
      }
      function De(e, n, t, r, i) {
        e.sendMessage(
          new c.Message({
            type: "new-history-item",
            payload: {
              documentId: n,
              operationFailed: t,
              operationName: i,
              operationType: r,
            },
          })
        );
      }
      function Me(e, n, t) {
        e.sendMessage(
          new c.Message({
            type: "new-result",
            payload: { documentId: n, result: t },
          })
        );
      }
      function Le(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function _e(e) {
        for (var n = 1; n < arguments.length; n++) {
          var t = null != arguments[n] ? arguments[n] : {};
          n % 2
            ? Le(Object(t), !0).forEach(function (n) {
                Re(e, n, t[n]);
              })
            : Object.getOwnPropertyDescriptors
            ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t))
            : Le(Object(t)).forEach(function (n) {
                Object.defineProperty(
                  e,
                  n,
                  Object.getOwnPropertyDescriptor(t, n)
                );
              });
        }
        return e;
      }
      function Re(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      var Ce = {};
      function Ve() {
        Ln("variables-cache-delete-item", function (e) {
          !(function (e) {
            Ce = _e(_e({}, Ce), {}, Re({}, e, void 0));
          })(e.payload.documentId);
        }),
          Ln("variables-cache-set-item", function (e) {
            var n = e.payload;
            !(function (e, n) {
              Ce = _e(_e({}, Ce), {}, Re({}, e, n));
            })(n.documentId, n.variables);
          });
      }
      function qe(e) {
        return Ce[e];
      }
      function Ge(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function Ae(e) {
        for (var n = 1; n < arguments.length; n++) {
          var t = null != arguments[n] ? arguments[n] : {};
          n % 2
            ? Ge(Object(t), !0).forEach(function (n) {
                $e(e, n, t[n]);
              })
            : Object.getOwnPropertyDescriptors
            ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t))
            : Ge(Object(t)).forEach(function (n) {
                Object.defineProperty(
                  e,
                  n,
                  Object.getOwnPropertyDescriptor(t, n)
                );
              });
        }
        return e;
      }
      function Qe(e, n, t, r, i, a, o) {
        try {
          var c = e[a](o),
            s = c.value;
        } catch (u) {
          return void t(u);
        }
        c.done ? n(s) : Promise.resolve(s).then(r, i);
      }
      function Je(e, n) {
        for (var t = 0; t < n.length; t++) {
          var r = n[t];
          (r.enumerable = r.enumerable || !1),
            (r.configurable = !0),
            "value" in r && (r.writable = !0),
            Object.defineProperty(e, r.key, r);
        }
      }
      function $e(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      var Be = (function () {
          function e() {
            !(function (e, n) {
              if (!(e instanceof n))
                throw new TypeError("Cannot call a class as a function");
            })(this, e),
              $e(this, "runningOperations", {});
          }
          var n, t, i;
          return (
            (n = e),
            (t = [
              {
                key: "isExecuting",
                value: function (e) {
                  return !!this.runningOperations[e.session.documentId];
                },
              },
              {
                key: "cancel",
                value: function (e) {
                  var n = e.session.documentId,
                    t = this.runningOperations[n];
                  t &&
                    (this.stop(e, n),
                    pe(
                      e,
                      t.operationName
                        ? "Cancelled "
                            .concat(t.operationType, ' operation "')
                            .concat(t.operationName, '".')
                        : "Cancelled ".concat(t.operationType, " operation.")
                    ));
                },
              },
              {
                key: "execute",
                value: function (e, n, t) {
                  var r = e.session;
                  if (
                    !this.runningOperations[r.documentId] &&
                    r &&
                    r.schemaEndpoint.length > 0
                  ) {
                    var i = r.documentId,
                      a = _(i) || {};
                    this.executeCore(
                      e,
                      {
                        documentId: i,
                        httpOptions: {
                          headers: a,
                          uri: r.schemaEndpoint,
                          useGET: "GET" === r.httpMethod,
                          credentials: r.credentials,
                        },
                        operationName: t,
                      },
                      n
                    );
                  }
                },
              },
              {
                key: "executeCore",
                value: (function () {
                  var e,
                    n =
                      ((e = p.a.mark(function e(n, t, i) {
                        var a, o, s, u, d, l, f, m, v, y, h, b, g, k, O;
                        return p.a.wrap(
                          function (e) {
                            for (;;)
                              switch ((e.prev = e.next)) {
                                case 0:
                                  if (
                                    ((a = t.httpOptions),
                                    (o = t.documentId),
                                    (s = t.operationName),
                                    !(u = x(o)))
                                  ) {
                                    e.next = 20;
                                    break;
                                  }
                                  return (
                                    (d = Object(r.e)()),
                                    (l = qe(o) || {}),
                                    (this.runningOperations = Ae(
                                      Ae({}, this.runningOperations),
                                      {},
                                      $e({}, o, {
                                        operationName: s,
                                        operationType: i,
                                      })
                                    )),
                                    n.sendMessage(
                                      new c.Message({
                                        type: "operation-execution",
                                        payload: {
                                          documentId: o,
                                          isExecuting: !0,
                                          isSubscription: !1,
                                        },
                                      })
                                    ),
                                    pe(
                                      n,
                                      s
                                        ? "Starting "
                                            .concat(i, ' operation "')
                                            .concat(s, '".')
                                        : "Starting ".concat(i, " operation."),
                                      void 0,
                                      o
                                    ),
                                    (e.next = 10),
                                    ge(a, {
                                      operationName: s,
                                      query: u,
                                      variables: l,
                                    })
                                  );
                                case 10:
                                  (f = e.sent),
                                    (m = Object(r.e)()),
                                    (v = f.response),
                                    (y = v.parsedBody),
                                    (h = v.error),
                                    (b =
                                      !!h ||
                                      (y.errors && y.errors.length > 0) ||
                                      !1),
                                    De(n, o, b, i, s),
                                    Me(n, o, {
                                      payload: f,
                                      duration: Object(r.c)(d, m),
                                      operationName: s,
                                      operationType: i,
                                      success: !b,
                                      timestamp: d,
                                    }),
                                    (g = "query" === i ? "Query" : "Mutation"),
                                    (k = s ? '"'.concat(s, '" ') : ""),
                                    (O = "".concat(g, " operation ").concat(k)),
                                    b
                                      ? de(
                                          n,
                                          "".concat(O, " failed."),
                                          h || y.errors,
                                          o
                                        )
                                      : pe(
                                          n,
                                          "".concat(O, " succeeded."),
                                          void 0,
                                          o
                                        );
                                case 20:
                                  this.stop(n, o);
                                case 21:
                                case "end":
                                  return e.stop();
                              }
                          },
                          e,
                          this
                        );
                      })),
                      function () {
                        var n = this,
                          t = arguments;
                        return new Promise(function (r, i) {
                          var a = e.apply(n, t);
                          function o(e) {
                            Qe(a, r, i, o, c, "next", e);
                          }
                          function c(e) {
                            Qe(a, r, i, o, c, "throw", e);
                          }
                          o(void 0);
                        });
                      });
                  return function (e, t, r) {
                    return n.apply(this, arguments);
                  };
                })(),
              },
              {
                key: "stop",
                value: function (e, n) {
                  (this.runningOperations = Ae(
                    Ae({}, this.runningOperations),
                    {},
                    $e({}, n, void 0)
                  )),
                    e.sendMessage(
                      new c.Message({
                        type: "operation-execution",
                        payload: {
                          documentId: n,
                          isExecuting: !1,
                          isSubscription: !1,
                        },
                      })
                    );
                },
              },
            ]) && Je(n.prototype, t),
            i && Je(n, i),
            e
          );
        })(),
        Ue = t(400);
      function ze(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function Ke(e) {
        for (var n = 1; n < arguments.length; n++) {
          var t = null != arguments[n] ? arguments[n] : {};
          n % 2
            ? ze(Object(t), !0).forEach(function (n) {
                We(e, n, t[n]);
              })
            : Object.getOwnPropertyDescriptors
            ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t))
            : ze(Object(t)).forEach(function (n) {
                Object.defineProperty(
                  e,
                  n,
                  Object.getOwnPropertyDescriptor(t, n)
                );
              });
        }
        return e;
      }
      function He(e, n) {
        for (var t = 0; t < n.length; t++) {
          var r = n[t];
          (r.enumerable = r.enumerable || !1),
            (r.configurable = !0),
            "value" in r && (r.writable = !0),
            Object.defineProperty(e, r.key, r);
        }
      }
      function We(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      var Xe = (function () {
          function e() {
            !(function (e, n) {
              if (!(e instanceof n))
                throw new TypeError("Cannot call a class as a function");
            })(this, e),
              We(this, "runningSubscriptions", {});
          }
          var n, t, i;
          return (
            (n = e),
            (i = [
              {
                key: "getSchemaEndpoint",
                value: function (e) {
                  return !e.subscriptionEndpoint && e.schemaEndpoint
                    ? e.schemaEndpoint.replace(/^http/i, "ws")
                    : e.subscriptionEndpoint || "";
                },
              },
            ]),
            (t = [
              {
                key: "isExecuting",
                value: function (e) {
                  return !!this.runningSubscriptions[e.session.documentId];
                },
              },
              {
                key: "cancel",
                value: function (e) {
                  var n = e.session;
                  if (n) {
                    var t = this.runningSubscriptions[n.documentId];
                    if (t) {
                      var r = t.operationName,
                        i = t.unsubscribe;
                      pe(
                        e,
                        r
                          ? 'Cancelled subscription operation "'.concat(r, '".')
                          : "Cancelled subscription operation.",
                        void 0,
                        n.documentId
                      ),
                        i(),
                        (this.runningSubscriptions = Ke(
                          Ke({}, this.runningSubscriptions),
                          {},
                          We({}, n.documentId, void 0)
                        )),
                        e.sendMessage(
                          new c.Message({
                            type: "operation-execution",
                            payload: {
                              documentId: n.documentId,
                              isExecuting: !1,
                              isSubscription: !0,
                            },
                          })
                        );
                    }
                  }
                },
              },
              {
                key: "execute",
                value: function (n, t, r) {
                  var i = n.session;
                  if (i && !this.runningSubscriptions[i.documentId]) {
                    var a = _(i.documentId) || {},
                      o = e.getSchemaEndpoint(i);
                    o.length &&
                      this.executeCore(
                        n,
                        {
                          documentId: i.documentId,
                          httpOptions: {
                            headers: a,
                            uri: o,
                            useGET: "GET" === i.httpMethod,
                            credentials: i.credentials,
                          },
                          operationName: r,
                        },
                        t
                      );
                  }
                },
              },
              {
                key: "executeCore",
                value: function (e, n, t) {
                  var i = this,
                    a = n.documentId,
                    o = n.httpOptions,
                    s = n.operationName,
                    u = x(a);
                  if (u) {
                    pe(
                      e,
                      s
                        ? 'Subscribing to subscription "'.concat(s, '".')
                        : "Subscribing to subscription.",
                      void 0,
                      a
                    );
                    var d = qe(a) || {},
                      p = new Ue.SubscriptionClient(o.uri, { lazy: !0 });
                    p.onConnected(function () {
                      (i.runningSubscriptions = Ke(
                        Ke({}, i.runningSubscriptions),
                        {},
                        We({}, a, l)
                      )),
                        e.sendMessage(
                          new c.Message({
                            type: "operation-execution",
                            payload: {
                              documentId: a,
                              isExecuting: !0,
                              isSubscription: !0,
                            },
                          })
                        ),
                        De(e, a, !1, t, s);
                    }),
                      p.onError(function (n) {
                        de(
                          e,
                          s
                            ? 'Subscribing to subscription "'.concat(
                                s,
                                '" failed.'
                              )
                            : "Subscribing to subscription failed.",
                          n,
                          a
                        ),
                          De(e, a, !0, t, s);
                      });
                    var l = p
                      .request({ operationName: s, query: u, variables: d })
                      .subscribe({
                        next: function (n) {
                          var i = (n && n.errors && n.errors.length > 0) || !1;
                          i
                            ? de(
                                e,
                                "New subscription result arrived.",
                                n.errors,
                                a
                              )
                            : pe(
                                e,
                                "New subscription result arrived.",
                                void 0,
                                a
                              ),
                            Me(e, a, {
                              payload: { raw: n },
                              duration: 0,
                              operationName: s,
                              operationType: t,
                              success: !i,
                              timestamp: Object(r.e)(),
                            });
                        },
                        error: function (n) {
                          de(
                            e,
                            "A subscription result ended up in an error.",
                            n,
                            a
                          ),
                            Me(e, a, {
                              payload: { error: n },
                              duration: 0,
                              operationName: s,
                              operationType: t,
                              success: !1,
                              timestamp: Object(r.e)(),
                            });
                        },
                        complete: function () {
                          pe(
                            e,
                            s
                              ? 'Unsubscribing from subscription "'.concat(
                                  s,
                                  '".'
                                )
                              : "Unsubscribing from subscription.",
                            void 0,
                            a
                          );
                        },
                      });
                  }
                },
              },
            ]) && He(n.prototype, t),
            i && He(n, i),
            e
          );
        })(),
        Ye = new Be(),
        Ze = new Xe(),
        en = new Map([
          ["query", Ye],
          ["mutation", Ye],
          ["subscription", Ze],
        ]);
      function nn() {
        Ln("cancel-operation-execution", function (e, n) {
          !(function (e) {
            Ye.isExecuting(e) && Ye.cancel(e),
              Ze.isExecuting(e) && Ze.cancel(e);
          })(n);
        }),
          Ln("begin-operation-execution", function (e, n) {
            !(function (e, n) {
              if (!Ye.isExecuting(e) && !Ze.isExecuting(e)) {
                var t = x(e.session.documentId);
                if (t) {
                  var r = (function (e, n) {
                      var t = e.definitions
                          .map(function (e) {
                            return e;
                          })
                          .filter(function (e) {
                            return !!e;
                          }),
                        r =
                          1 === t.length
                            ? t[0]
                            : t.find(function (e) {
                                return (e.name && e.name.value === n) || !1;
                              });
                      return r ? r.operation : "query";
                    })(t, n),
                    i = en.get(r);
                  i && i.execute(e, r, n);
                }
              }
            })(n, e.payload.operationName);
          });
      }
      var tn = t(22);
      function rn(e, n, t, r, i, a, o) {
        try {
          var c = e[a](o),
            s = c.value;
        } catch (u) {
          return void t(u);
        }
        c.done ? n(s) : Promise.resolve(s).then(r, i);
      }
      function an(e) {
        return function () {
          var n = this,
            t = arguments;
          return new Promise(function (r, i) {
            var a = e.apply(n, t);
            function o(e) {
              rn(a, r, i, o, c, "next", e);
            }
            function c(e) {
              rn(a, r, i, o, c, "throw", e);
            }
            o(void 0);
          });
        };
      }
      function on() {
        Ln(
          "fetch-schema-field-type",
          (function () {
            var e = an(
              p.a.mark(function e(n, t) {
                var r, i, a, o, s;
                return p.a.wrap(function (e) {
                  for (;;)
                    switch ((e.prev = e.next)) {
                      case 0:
                        return (
                          (r = n.id),
                          (i = n.payload),
                          (a = n.type),
                          (e.next = 3),
                          Y(t)
                        );
                      case 3:
                        (o = e.sent),
                          (s = void 0),
                          o && (s = cn(o.schema, i.typeName, i.fieldName)),
                          t.sendMessage(
                            new c.Message({ id: r, type: a, payload: s })
                          );
                      case 7:
                      case "end":
                        return e.stop();
                    }
                }, e);
              })
            );
            return function (n, t) {
              return e.apply(this, arguments);
            };
          })()
        ),
          Ln(
            "fetch-schema-field-types",
            (function () {
              var e = an(
                p.a.mark(function e(n, t) {
                  var r, i, a, o, s;
                  return p.a.wrap(function (e) {
                    for (;;)
                      switch ((e.prev = e.next)) {
                        case 0:
                          return (
                            (r = n.id),
                            (i = n.payload),
                            (a = n.type),
                            (e.next = 3),
                            Y(t)
                          );
                        case 3:
                          (o = e.sent),
                            (s = void 0),
                            o && (s = sn(o.schema, i)),
                            t.sendMessage(
                              new c.Message({ id: r, type: a, payload: s })
                            );
                        case 7:
                        case "end":
                          return e.stop();
                      }
                  }, e);
                })
              );
              return function (n, t) {
                return e.apply(this, arguments);
              };
            })()
          ),
          Ln(
            "fetch-schema-type",
            (function () {
              var e = an(
                p.a.mark(function e(n, t) {
                  var r, i, a, o, s;
                  return p.a.wrap(function (e) {
                    for (;;)
                      switch ((e.prev = e.next)) {
                        case 0:
                          return (
                            (r = n.id),
                            (i = n.payload),
                            (a = n.type),
                            (e.next = 3),
                            Y(t)
                          );
                        case 3:
                          (o = e.sent),
                            (s = void 0),
                            o && (s = dn(o.schema, i)),
                            t.sendMessage(
                              new c.Message({ id: r, type: a, payload: s })
                            );
                        case 7:
                        case "end":
                          return e.stop();
                      }
                  }, e);
                })
              );
              return function (n, t) {
                return e.apply(this, arguments);
              };
            })()
          ),
          Ln(
            "fetch-schema-types",
            (function () {
              var e = an(
                p.a.mark(function e(n, t) {
                  var r, i, a, o;
                  return p.a.wrap(function (e) {
                    for (;;)
                      switch ((e.prev = e.next)) {
                        case 0:
                          return (r = n.id), (i = n.type), (e.next = 3), Y(t);
                        case 3:
                          (a = e.sent),
                            (o = void 0),
                            a && (o = un(a.schema)),
                            t.sendMessage(
                              new c.Message({ id: r, type: i, payload: o })
                            );
                        case 7:
                        case "end":
                          return e.stop();
                      }
                  }, e);
                })
              );
              return function (n, t) {
                return e.apply(this, arguments);
              };
            })()
          );
      }
      function cn(e, n, t) {
        var r = e.getType(n);
        if (r)
          return (function (e, n, t) {
            if (n instanceof tn.b) {
              var r = n.getFields()[t];
              if (r)
                return {
                  id: "".concat(n.name, "-").concat(r.name),
                  name: r.name,
                  kind: "field",
                  description: r.description || void 0,
                  valueType: fn(e, r.type),
                  defaultValue: r.defaultValue,
                };
            } else if (n instanceof tn.c || n instanceof tn.f) {
              var i = n.getFields()[t];
              if (i) {
                var a =
                  0 === i.args.length
                    ? void 0
                    : i.args.map(function (t) {
                        var r = fn(e, t.type);
                        return {
                          id: ""
                            .concat(n.name, "-")
                            .concat(i.name, "-")
                            .concat(t.name),
                          name: t.name,
                          kind: "argument",
                          description: t.description || void 0,
                          defaultValue: t.defaultValue || void 0,
                          type: r,
                        };
                      });
                return {
                  id: "".concat(n.name, "-").concat(i.name),
                  name: i.name,
                  kind: "field",
                  description: i.description || void 0,
                  arguments: a,
                  valueType: fn(e, i.type),
                  isDeprecated: i.isDeprecated,
                  deprecationReason: i.deprecationReason || void 0,
                };
              }
            }
            return;
          })(e, r, t);
      }
      function sn(e, n) {
        var t = e.getType(n);
        if (t)
          return (function (e, n) {
            var t = [];
            if (n instanceof tn.b) {
              var r = n.getFields();
              for (var i in r) {
                var a = r[i];
                t.push({
                  id: "".concat(n.name, "-").concat(a.name),
                  name: a.name,
                  kind: "field",
                  description: a.description || void 0,
                  valueType: fn(e, a.type),
                  defaultValue: a.defaultValue,
                });
              }
            } else if (n instanceof tn.c || n instanceof tn.f) {
              var o = n.getFields(),
                c = function (r) {
                  var i = o[r],
                    a =
                      0 === i.args.length
                        ? void 0
                        : i.args.map(function (t) {
                            var r = fn(e, t.type);
                            return {
                              id: ""
                                .concat(n.name, "-")
                                .concat(i.name, "-")
                                .concat(t.name),
                              name: t.name,
                              kind: "argument",
                              description: t.description || void 0,
                              defaultValue: t.defaultValue || void 0,
                              type: r,
                            };
                          });
                  t.push({
                    id: "".concat(n.name, "-").concat(i.name),
                    name: i.name,
                    kind: "field",
                    description: i.description || void 0,
                    arguments: a,
                    valueType: fn(e, i.type),
                    isDeprecated: i.isDeprecated,
                    deprecationReason: i.deprecationReason || void 0,
                  });
                };
              for (var s in o) c(s);
            }
            return t;
          })(e, t);
      }
      function un(e) {
        var n = [],
          t = e.getQueryType(),
          r = e.getMutationType(),
          i = e.getSubscriptionType();
        return (
          t && n.push(ln(e, t)),
          r && n.push(ln(e, r)),
          i && n.push(ln(e, i)),
          0 === n.length ? void 0 : n
        );
      }
      function dn(e, n) {
        var t = e.getType(n);
        if (t) return ln(e, t);
      }
      function pn(e) {
        return e instanceof tn.a
          ? "enum"
          : e instanceof tn.b
          ? "input-object"
          : e instanceof tn.c
          ? "interface"
          : e instanceof tn.g
          ? "scalar"
          : e instanceof tn.h
          ? "union"
          : e instanceof tn.d || e instanceof tn.e
          ? pn(e.ofType)
          : "object";
      }
      function ln(e, n) {
        var t, r, i;
        return (
          n instanceof tn.a &&
            (t = n.getValues().map(function (e) {
              return {
                id: "".concat(n.name, "-").concat(e.name),
                name: e.name,
                kind: "enum-value",
                description: e.description || void 0,
                value: e.value,
                isDeprecated: e.isDeprecated,
                deprecationReason: e.deprecationReason || void 0,
              };
            })),
          n instanceof tn.c &&
            (i = e.getPossibleTypes(n).map(function (n) {
              return fn(e, n);
            })),
          n instanceof tn.f &&
            (r = n.getInterfaces().map(function (e) {
              return {
                id: e.name,
                name: e.name,
                kind: "type",
                description: e.description || void 0,
                typeKind: "interface",
              };
            })),
          n instanceof tn.h &&
            (i = n.getTypes().map(function (n) {
              return fn(e, n);
            })),
          {
            id: n.name,
            name: n.name,
            kind: "type",
            description: n.description || void 0,
            typeKind: pn(n),
            enumValues: t,
            interfaces: r,
            types: i,
          }
        );
      }
      function fn(e, n) {
        var t,
          r = !1,
          i = !0;
        return (
          n instanceof tn.e
            ? ((i = !1),
              n.ofType instanceof tn.d
                ? ((t = n.ofType.ofType), (r = !0))
                : (t = n.ofType))
            : n instanceof tn.d
            ? ((t = n.ofType), (r = !0))
            : (t = n),
          {
            kind: "value-type",
            isList: r,
            isOptional: i,
            type: t instanceof tn.d || t instanceof tn.e ? fn(e, t) : ln(e, t),
          }
        );
      }
      function mn(e, n) {
        var t = Object.keys(e);
        if (Object.getOwnPropertySymbols) {
          var r = Object.getOwnPropertySymbols(e);
          n &&
            (r = r.filter(function (n) {
              return Object.getOwnPropertyDescriptor(e, n).enumerable;
            })),
            t.push.apply(t, r);
        }
        return t;
      }
      function vn(e) {
        for (var n = 1; n < arguments.length; n++) {
          var t = null != arguments[n] ? arguments[n] : {};
          n % 2
            ? mn(Object(t), !0).forEach(function (n) {
                yn(e, n, t[n]);
              })
            : Object.getOwnPropertyDescriptors
            ? Object.defineProperties(e, Object.getOwnPropertyDescriptors(t))
            : mn(Object(t)).forEach(function (n) {
                Object.defineProperty(
                  e,
                  n,
                  Object.getOwnPropertyDescriptor(t, n)
                );
              });
        }
        return e;
      }
      function yn(e, n, t) {
        return (
          n in e
            ? Object.defineProperty(e, n, {
                value: t,
                enumerable: !0,
                configurable: !0,
                writable: !0,
              })
            : (e[n] = t),
          e
        );
      }
      function hn(e, n, t, r, i, a, o) {
        try {
          var c = e[a](o),
            s = c.value;
        } catch (u) {
          return void t(u);
        }
        c.done ? n(s) : Promise.resolve(s).then(r, i);
      }
      function bn(e) {
        return function () {
          var n = this,
            t = arguments;
          return new Promise(function (r, i) {
            var a = e.apply(n, t);
            function o(e) {
              hn(a, r, i, o, c, "next", e);
            }
            function c(e) {
              hn(a, r, i, o, c, "throw", e);
            }
            o(void 0);
          });
        };
      }
      var gn = new Map();
      function kn() {
        Ln("cancel-schema-fetching", function (e, n) {
          !(function (e) {
            var n = gn.get(e.id);
            (null === n || void 0 === n ? void 0 : n.running) &&
              (n.backoff &&
                pe(e, "Cancelled schema fetching.", void 0, n.currentId),
              n.timeout &&
                pe(e, "Cancelled schema polling.", void 0, n.currentId),
              En(e));
          })(n);
        }),
          Ln("fetch-schema", function (e, n) {
            !(function (e) {
              On.apply(this, arguments);
            })(n);
          }),
          Ln("refetch-schema", function (e, n) {
            !(function (e) {
              if (!jn(e.id).running) {
                var n = e.session;
                if (n.schemaEndpoint.length > 0 && !n.enableSchemaPolling) {
                  var t = {
                    headers: _(n.documentId) || {},
                    uri: n.schemaEndpoint,
                    useGETForQueries: "GET" === n.httpMethod,
                    credentials: n.credentials,
                  };
                  Tn(e.id, { running: !0 }), pe(e, "Reload schema."), Pn(e, t);
                }
              }
            })(n);
          });
      }
      function On() {
        return (On = bn(
          p.a.mark(function e(n) {
            var t, r, i, a;
            return p.a.wrap(function (e) {
              for (;;)
                switch ((e.prev = e.next)) {
                  case 0:
                    if (jn(n.id).running) {
                      e.next = 14;
                      break;
                    }
                    if (!((t = n.session).schemaEndpoint.length > 0)) {
                      e.next = 13;
                      break;
                    }
                    return (
                      (r = _(t.documentId) || {}),
                      (i = {
                        headers: r,
                        uri: t.schemaEndpoint,
                        useGETForQueries: "GET" === t.httpMethod,
                        credentials: t.credentials,
                      }),
                      (e.next = 8),
                      Y(n)
                    );
                  case 8:
                    (a = e.sent) &&
                      (se(n, {
                        endpoint: t.schemaEndpoint,
                        exists: !0,
                        hash: a.hash,
                        hasMutationType: !!a.schema.getMutationType(),
                        hasSubscriptionType: !!a.schema.getSubscriptionType(),
                      }),
                      pe(n, "Fetched schema from cache.")),
                      t.enableSchemaPolling
                        ? (Tn(n.id, { running: !0 }),
                          pe(n, "Started schema polling."),
                          Fn(n, i, t.schemaPollingInterval || 2e4))
                        : a ||
                          (Tn(n.id, { running: !0 }),
                          pe(n, "Started schema fetching."),
                          Pn(n, i)),
                      (e.next = 14);
                    break;
                  case 13:
                    se(n, {
                      endpoint: t.schemaEndpoint,
                      exists: !1,
                      hasMutationType: !1,
                      hasSubscriptionType: !1,
                    });
                  case 14:
                  case "end":
                    return e.stop();
                }
            }, e);
          })
        )).apply(this, arguments);
      }
      function wn(e, n, t, r) {
        return Sn.apply(this, arguments);
      }
      function Sn() {
        return (Sn = bn(
          p.a.mark(function e(n, t, r, i) {
            var a, o;
            return p.a.wrap(function (e) {
              for (;;)
                switch ((e.prev = e.next)) {
                  case 0:
                    return (a = t.currentRequestId), (e.next = 3), je(i);
                  case 3:
                    if (((o = e.sent), r !== a)) {
                      e.next = 14;
                      break;
                    }
                    if (!xn(o)) {
                      e.next = 11;
                      break;
                    }
                    return (e.next = 8), ee(n, o);
                  case 8:
                    pe(n, "Fetched schema successfully."), (e.next = 14);
                    break;
                  case 11:
                    return (
                      se(n, {
                        endpoint: i.uri,
                        exists: !1,
                        hasMutationType: !1,
                        hasSubscriptionType: !1,
                      }),
                      de(n, "Fetching schema failed.", o),
                      e.abrupt("return", !1)
                    );
                  case 14:
                    return e.abrupt("return", !0);
                  case 15:
                  case "end":
                    return e.stop();
                }
            }, e);
          })
        )).apply(this, arguments);
      }
      function jn(e) {
        return gn.get(e) || { fetching: !1, running: !1 };
      }
      function xn(e) {
        return "__schema" in e;
      }
      function Nn(e, n) {
        e.sendMessage(
          new c.Message({ type: "is-schema-fetching", payload: n })
        );
      }
      function Tn(e, n) {
        var t = vn(vn({}, jn(e)), n);
        return gn.set(e, t), t;
      }
      function Pn(e, n) {
        var t = e.id,
          i = e.session.documentId,
          a = Object(r.b)(),
          o = new y(function (r) {
            var i = jn(t);
            return (
              r > 0
                ? pe(e, "Retry schema fetching.")
                : ((i = Tn(t, { fetching: !0 })), Nn(e, i.fetching)),
              wn(e, i, a, n)
            );
          });
        Tn(t, { backoff: o, currentId: i, currentRequestId: a }),
          o.start(function () {
            var n = jn(t);
            a === n.currentRequestId && En(e);
          });
      }
      function Fn(e, n, t) {
        var i = Object(r.b)(),
          a = Tn(e.id, {
            currentId: e.session.documentId,
            currentRequestId: i,
            timeout: window.setTimeout(function () {
              Fn(e, n, t);
            }, t),
          });
        wn(e, a, i, n);
      }
      function En(e) {
        var n = jn(e.id);
        n.backoff && n.backoff.stop(),
          n.timeout && window.clearTimeout(n.timeout),
          Nn(
            e,
            (n = Tn(e.id, {
              backoff: void 0,
              currentId: void 0,
              currentRequestId: void 0,
              fetching: !1,
              running: !1,
              timeout: void 0,
            })).fetching
          );
      }
      var In = [],
        Dn = [];
      function Mn(e) {
        var n = new F(e);
        In.push(n),
          j(),
          L(),
          Ve(),
          oe(),
          on(),
          nn(),
          kn(),
          (e.onmessage = function (e) {
            var t = e.data;
            !(function (e, n) {
              Dn.filter(function (e) {
                return e.type === n.type;
              }).forEach(function (t) {
                return (0, t.handle)(n, e);
              });
            })(n, t);
          }),
          e.start();
      }
      function Ln(e, n) {
        Dn.push({ type: e, handle: n });
      }
      self.onconnect = function (e) {
        return Mn(e.ports[0]);
      };
    },
  },
  [[842, 5, 0, 8]],
]);
//# sourceMappingURL=schema.worker.15ea7f05.chunk.js.map
