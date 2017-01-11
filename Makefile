TARGET      = main
BUILD       = .build

ifdef DEBUG
CFLAGS     += -define:DEBUG
endif

ifdef CSHARP_VERSION
CFLAGS     += -langversion:$(CSHARP_VERSION)
endif

CC          = mcs
RUN         = mono
ARGUMENTS  ?=
NUNIT       = nunit-console -nologo

all: run

$(BUILD)/%.exe: src/*.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $^

run: $(BUILD)/$(TARGET).exe
	@$(RUN) $< $(ARGUMENTS)

$(BUILD)/test.dll: test/*.cs src/*.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -t:library -r:nunit.framework.dll -out:$@ $^

test: $(BUILD)/test.dll
	@$(NUNIT) $<

clean:
	@rm -rf $(BUILD) TestResult.xml

-include Makefile.local
